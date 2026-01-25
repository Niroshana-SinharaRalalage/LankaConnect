# Refocus Prompt - When Agent Loses Focus

**Use this when an agent gets distracted, forgets best practices, or loses track of original task.**

---

## 🎯 REFOCUS CHECKLIST

When you notice agent drifting, paste this prompt:

```
REFOCUS CHECKPOINT:

1. ✅ Re-read CLAUDE.md Sections 1, 2, 9 (Senior Engineer mindset, TDD, Module Standards)
2. ✅ Check todo list - what task number are we on? (e.g., "Task 3 of 7")
3. ✅ Recall original objective: [Building Marketplace Product domain model]
4. ✅ Verify current status:
   - What have we completed?
   - What's next?
   - Are we still following TDD? (tests first!)
   - Are we still following Clean Architecture?
5. ✅ Resume original task or explain if we need to adjust plan

DO NOT proceed until you've completed this checklist.
```

---

## 🚨 RED FLAGS (When to Use This Prompt)

Use REFOCUS PROMPT when agent:
- Switches to bug fix and doesn't return to original task
- Forgets to write tests (TDD violation)
- Suggests shortcuts or quick patches (not senior engineer mindset)
- Creates files in wrong locations (violates module structure)
- Forgets to update tracking docs (PROGRESS_TRACKER.md)
- Asks "what should I do next?" when there's a clear todo list
- Breaks existing functionality (UI, Events module)

---

## 📋 ORIGINAL TASK TEMPLATES

### For Marketplace Agent
```
Original Task: Build complete Marketplace module
Current Phase: [Domain/Application/Infrastructure/API]
Current Feature: [Product catalog/Shopping cart/Checkout/Orders]
Next Step: [Check todo list for current task]

MANDATORY RULES:
- TDD: Tests first, then implementation
- Clean Architecture: Domain → Application → Infrastructure → API
- 90%+ test coverage before moving to next feature
- Update PROGRESS_TRACKER.md after each major milestone
- Reference UI_STYLE_GUIDE.md for any UI work
```

### For Business Profile Agent
```
Original Task: Build complete Business Profile module
Current Phase: [Domain/Application/Infrastructure/API]
Current Feature: [Business entities/Approval workflow/Admin panel]
Next Step: [Check todo list for current task]

MANDATORY RULES:
- TDD: Tests first, then implementation
- Clean Architecture: Domain → Application → Infrastructure → API
- 90%+ test coverage before moving to next feature
- Update PROGRESS_TRACKER.md after each major milestone
- Reference UI_STYLE_GUIDE.md for any UI work
```

### For Forum Agent
```
Original Task: Build complete Forum module
Current Phase: [Domain/Application/Infrastructure/API]
Current Feature: [Forum entities/Content moderation/Discussion threads]
Next Step: [Check todo list for current task]

MANDATORY RULES:
- TDD: Tests first, then implementation
- Clean Architecture: Domain → Application → Infrastructure → API
- 90%+ test coverage before moving to next feature
- Update PROGRESS_TRACKER.md after each major milestone
- Reference UI_STYLE_GUIDE.md for any UI work
```

### For Frontend Agent
```
Original Task: Build all UI pages for Marketplace, Business, Forum
Current Phase: [Marketplace pages/Business pages/Forum pages]
Current Feature: [Product catalog/Business directory/Forum threads]
Next Step: [Check todo list for current task]

MANDATORY RULES:
- Use ONLY components from UI_STYLE_GUIDE.md
- TypeScript for all code
- Zustand for state management
- Test each page before moving to next
- Update PROGRESS_TRACKER.md after each page complete
```

---

## 🔄 TASK SWITCHING PROTOCOL

If you need to switch from original task to something else (bug fix, urgent issue):

### When Switching AWAY from Original Task:
```
Agent: "Pausing current task: [Product domain model, Task 2 of 7]
       Switching to: [Fix deployment bug]
       Will return to original task after fix."
```

### When Switching BACK to Original Task:
```
You: "REFOCUS: Check CLAUDE.md, check todo list, resume Task 2 of 7: Product domain model"
Agent: *Re-reads CLAUDE.md, checks todos, resumes original work*
```

---

## 💡 BEST PRACTICES for Multi-Agent Conversations

### Keep Conversations Under 50 Messages
- ✅ Start conversation with clear objective
- ✅ Create detailed todo list (7-10 tasks)
- ✅ Work through todos systematically
- ✅ Mark complete and close conversation when feature done
- ✅ Start NEW conversation for next feature

### Use REFOCUS PROMPT Every 20-30 Messages
Even if agent seems on track, periodic refocus prevents drift:
```
Message 1-30: Working on Product domain
Message 31: REFOCUS CHECKPOINT (paste checklist)
Message 32-60: Continue Product domain
Message 61: REFOCUS CHECKPOINT (paste checklist)
```

### Always Return to Todo List
Todo list is the **anchor**:
- Agent forgets what's next → "Check todo list"
- Agent gets distracted → "Check todo list, resume Task X"
- Agent asks "what now?" → "You have 5 pending tasks in the todo list"

---

## 📝 EXAMPLE REFOCUS CONVERSATION

**Agent (drifting):**
> "I fixed that bug. What should we work on next?"

**You (refocus):**
```
REFOCUS CHECKPOINT:

1. Re-read CLAUDE.md Sections 1, 2, 9
2. Check todo list - you're on Task 3 of 7: "Implement Product command handlers"
3. Original objective: Building Marketplace Product domain model with TDD
4. Status check:
   - Completed: Product aggregate ✅
   - Completed: ProductRepository interface ✅
   - Next: Product command handlers (CreateProduct, UpdateInventory)
5. Resume Task 3: Write tests FIRST for CreateProductCommandHandler

DO NOT proceed until you've reviewed CLAUDE.md and confirmed next steps.
```

**Agent (refocused):**
> "Understood. I've re-read CLAUDE.md. Following TDD, I'll write tests first for CreateProductCommandHandler. Here's the test..."

---

## 🎯 SUMMARY

**Problem:** Agents lose focus in long conversations

**Solution:** Use this REFOCUS PROMPT regularly

**When to Use:**
- Every 20-30 messages (preventive)
- After task switches (bug fix, tangent)
- When agent asks "what's next?" (should check todos)
- When agent forgets best practices (TDD, Clean Architecture)

**How to Use:**
1. Copy "REFOCUS CHECKPOINT" checklist
2. Paste into conversation
3. Agent re-reads CLAUDE.md, checks todos, resumes work

**Keep this document open** in all 4 agent windows for quick reference.
