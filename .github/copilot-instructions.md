# Global Copilot Instructions
### Applies to all projects targeting .NET 10+

---

## 🧱 Core Principles
- **robust**, **low‑allocation**, **high‑performance**.
- Prioritize simplicity first during code reviews and changes; favor straightforward, maintainable.
- Use modern .NET idioms.

---

## 🧩 Architecture Standards
Copilot should default to **Clean Architecture**:
Copilot must justify any deviation.

---

## ⚙️ Performance Requirements
Copilot must always consider performance:

- Prefer **Span<T>**, **Memory<T>**, **ReadOnlySpan<T>**, **ValueTask**.
- Avoid unnecessary allocations.
- Prefer **async** over threads; avoid blocking calls.

---

## 🧭 Copilot Workflow Rules

### 1. File Access Rules
Copilot may:

- Read any file in the workspace.
- Write new files only after approval.
- Modify existing files only after approval.

### 2. Output Preferences
- Show only modified code blocks (diffs) instead of full-file code dumps unless a full-file output is necessary.
- Ask for confirmation before providing full-file outputs when unsure.

---

## 🧠 Copilot Behavior Expectations
Copilot must:

- Provide **best‑practice, production‑ready** solutions.
- Explain trade‑offs.
- Ask questions when uncertain.

---

## 🧹 What Copilot Must Avoid
- Hidden side effects.
- Clever but fragile code.
- Outdated patterns (e.g., `Task.Run` wrappers).
- Over‑engineering or unnecessary abstractions.

---

## 🏁 Final Rule
**If Copilot is unsure, it must ask clarifying questions before generating code.**
