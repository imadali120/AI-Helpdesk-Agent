# IB220336 - Dorada 1: Learning Mechanism

## Pregled

Implementiran LEARN mehanizam koji omogućava agentu da uči iz human feedbacka i prilagođava confidence thresholds po kategoriji.

---

## 1. Feedback Persistence

### Prije
- Feedback se čuvao **in-memory** (`ConcurrentBag`)
- Gubio se pri restartu aplikacije
- Nije sadržavao informaciju o originalnoj klasifikaciji

```csharp
// FeedbackService.cs (staro)
private readonly ConcurrentBag<FeedbackEntry> _feedbackEntries = new();
```

### Poslije
- Feedback se čuva u **SQLite bazi** (tabela `FeedbackEntries`)
- Sadrži original vs correct klasifikaciju
- Sadrži flag `WasCategoryCorrect` za learning

```csharp
// FeedbackService.cs (novo)
_context.FeedbackEntries.Add(feedbackEntry);
await _context.SaveChangesAsync(ct);
```

### Lokacija u kodu

| Fajl | Klasa/Metoda |
|------|--------------|
| `AiAgents.HelpdeskAgent/Domain/FeedbackEntry.cs` | `FeedbackEntry` (novi entitet) |
| `AiAgents.HelpdeskAgent/Application/FeedbackService.cs` | `SubmitFeedbackAsync()` |
| `AiAgents.HelpdeskAgent/Infrastructure/HelpdeskDbContext.cs` | `DbSet<FeedbackEntry>` |

### Schema

```csharp
public class FeedbackEntry
{
    public Guid Id { get; set; }
    public Guid TicketId { get; set; }
    public TicketCategory OriginalCategory { get; set; }
    public TicketCategory CorrectCategory { get; set; }
    public bool WasCategoryCorrect { get; set; }
    // ...
}
```

---

## 2. Policy Parameter Persistence

### Prije
- Jedan globalni `ConfidenceThreshold` (0.70) za sve kategorije
- Čuvan u `AgentSettings` tabeli
- Nije se mijenjao na osnovu feedbacka

```csharp
// DbSettingsProvider.cs (staro)
return settings.ConfidenceThreshold; // uvijek 0.70
```

### Poslije
- **Per-category** threshold u tabeli `CategoryPolicyParameters`
- Automatski se kreira pri prvom feedbacku za kategoriju
- Adjustuje se: wrong → +0.05, correct → -0.02

```csharp
// LearningService.cs (novo)
if (feedback.WasCategoryCorrect)
    policyParam.ConfidenceThreshold -= 0.02; // više confidence
else
    policyParam.ConfidenceThreshold += 0.05; // više opreza
```

### Lokacija u kodu

| Fajl | Klasa/Metoda |
|------|--------------|
| `AiAgents.HelpdeskAgent/Domain/CategoryPolicyParameter.cs` | `CategoryPolicyParameter` (novi entitet) |
| `AiAgents.HelpdeskAgent/Application/ILearningService.cs` | Interface (novi) |
| `AiAgents.HelpdeskAgent/Application/LearningService.cs` | `LearnFromFeedbackAsync()`, `GetEffectiveThresholdAsync()` |
| `AiAgents.HelpdeskAgent/Infrastructure/HelpdeskDbContext.cs` | `DbSet<CategoryPolicyParameter>` |

### Schema

```csharp
public class CategoryPolicyParameter
{
    public int Id { get; set; }
    public TicketCategory Category { get; set; }
    public double ConfidenceThreshold { get; set; }
    public int CorrectCount { get; set; }
    public int IncorrectCount { get; set; }
    // ...
}
```

---

## 3. Runner čita parametre

### Prije
- Runner koristi **globalni** threshold iz `AgentSettings`

```csharp
// TicketProcessingAgentRunner.cs (staro)
if (classification.Confidence < settings.ConfidenceThreshold)
    decision = AgentDecision.SentToReview;
```

### Poslije
- Runner koristi **category-specific** threshold iz `LearningService`

```csharp
// TicketProcessingAgentRunner.cs (novo)
var effectiveThreshold = await _learningService.GetEffectiveThresholdAsync(classification.Category, ct);

if (classification.Confidence < effectiveThreshold)
    decision = AgentDecision.SentToReview;
```

### Lokacija u kodu

| Fajl | Klasa/Metoda |
|------|--------------|
| `AiAgents.HelpdeskAgent/Application/TicketProcessingAgentRunner.cs` | `StepAsync()` linije 34-49 |
| `AiAgents.HelpdeskAgent/Application/LearningService.cs` | `GetEffectiveThresholdAsync()` |

### Flow

```
StepAsync()
    │
    ├─ Classify ticket → Category=Technical, Confidence=0.75
    │
    ├─ GetEffectiveThresholdAsync(Technical)
    │   └─ Query CategoryPolicyParameters WHERE Category=Technical
    │   └─ Return 0.80 (ili global 0.70 ako nema)
    │
    └─ if (0.75 < 0.80) → SentToReview
```

---

## 4. Demonstracija promjene ponašanja

### Prije feedbacka

```
Ticket: "App crashes on startup"
Category: Technical
Confidence: 0.75
Threshold: 0.70 (global)
Decision: AutoAssigned ✓
```

### Poslije 2x wrong feedback

```
Ticket: "App crashes on startup" (identičan)
Category: Technical
Confidence: 0.75 (ista)
Threshold: 0.80 (learned)
Decision: SentToReview ✗
```

### Lokacija demo koda

| Fajl | Endpoint |
|------|----------|
| `AiAgents.HelpdeskAgent.Web/Controllers/LearningDemoController.cs` | `POST /api/LearningDemo/run-demo` |

### Test komande

```bash
# Reset
curl -X POST http://localhost:5277/api/LearningDemo/reset

# Run demo (pokazuje before/after)
curl -X POST http://localhost:5277/api/LearningDemo/run-demo

# Provjeri thresholds
curl http://localhost:5277/api/LearningDemo/policy-parameters
```

---

## 5. Database Migration

### Fajl
`AiAgents.HelpdeskAgent/Migrations/20260204131504_AddLearningMechanism.cs`

### Kreira tabele

```sql
CREATE TABLE FeedbackEntries (
    Id TEXT PRIMARY KEY,
    TicketId TEXT NOT NULL,
    OriginalCategory INTEGER NOT NULL,
    CorrectCategory INTEGER NOT NULL,
    WasCategoryCorrect INTEGER NOT NULL,
    ...
);

CREATE TABLE CategoryPolicyParameters (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    Category INTEGER NOT NULL UNIQUE,
    ConfidenceThreshold REAL NOT NULL,
    CorrectCount INTEGER NOT NULL,
    IncorrectCount INTEGER NOT NULL,
    ...
);
```

---

## 6. DI Registration

### Fajl
`AiAgents.HelpdeskAgent.Web/Program.cs`

### Dodano

```csharp
builder.Services.AddScoped<ILearningService, LearningService>();
```

---

## Sumarni pregled fajlova

| Fajl | Akcija |
|------|--------|
| `Domain/FeedbackEntry.cs` | NOVO |
| `Domain/CategoryPolicyParameter.cs` | NOVO |
| `Application/ILearningService.cs` | NOVO |
| `Application/LearningService.cs` | NOVO |
| `Application/FeedbackService.cs` | IZMIJENJENO |
| `Application/TicketProcessingAgentRunner.cs` | IZMIJENJENO |
| `Infrastructure/HelpdeskDbContext.cs` | IZMIJENJENO |
| `Web/Program.cs` | IZMIJENJENO |
| `Web/Controllers/LearningDemoController.cs` | NOVO (demo) |
| `Migrations/...AddLearningMechanism.cs` | NOVO |

---

## Learning parametri

```csharp
// LearningService.cs
const double IncreaseOnWrong = 0.05;   // Pogrešno → povećaj threshold
const double DecreaseOnCorrect = 0.02; // Tačno → smanji threshold
const double MinThreshold = 0.30;      // Donja granica
const double MaxThreshold = 0.95;      // Gornja granica
```
