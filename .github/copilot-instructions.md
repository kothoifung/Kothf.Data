# Global Copilot Instructions  
### Applies to all projects targeting .NET 10+  
### Engineering Philosophy: Robust • High‑Performance • Production‑Ready

---

## 🧱 Core Principles
- All generated code must target **.NET 10+**.
- Solutions must be **robust**, **high‑performance**, and **production‑ready**.
- Prefer **modern .NET idioms**, **low‑allocation design**, and **clean architecture**.
- Avoid outdated APIs, legacy patterns, or unnecessary abstractions.
- All code must compile **warning‑free** (`TreatWarningsAsErrors=true`).

---

## ⚙️ Performance Requirements
Copilot must always consider performance:

- Prefer **Span<T>**, **Memory<T>**, **ReadOnlySpan<T>**, **ValueTask**.
- Avoid unnecessary allocations.
- Prefer **Channel<T>** over `ConcurrentQueue<T>` for pipelines.
- Prefer **async** over threads; avoid blocking calls.

---

## 🗄️ Data & EF Core 10
Copilot must:

- Use **batching**.
- Generate **parameterized SQL**.
- Avoid N+1 queries.

---

## 🧠 Copilot Behavior Expectations
Copilot must:

- Provide **best‑practice, production‑ready** solutions.
- Explain trade‑offs.
- Avoid clever but fragile code.
- Ask questions when uncertain.

---

## 🧹 What Copilot Must Avoid
- Blocking calls (`.Result`, `.Wait()`).
- Synchronous IO in async code.
- Static mutable state.
- Hidden side effects.
- Magic numbers or strings.
- Outdated patterns (e.g., `Task.Run` wrappers).
- Over‑engineering or unnecessary abstractions.

---

## 🏁 Final Rule
**If Copilot is unsure, it must ask clarifying questions before generating code.**
