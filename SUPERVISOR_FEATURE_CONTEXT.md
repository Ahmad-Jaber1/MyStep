# Supervisor Feature Context (Detailed)

This file documents what the Supervisor feature does today, the detailed user-visible flows, and an explicit, itemized implementation checklist that reproduces the full plan (so a future engineer or a new chat can continue without losing context).

---

## 1) Feature: What it does (User-facing)

- Adds a new role: Supervisor. Supervisors are separate accounts from students and are associated with one learning `Path` when created.
- Supervisors can invite/add students (by student email) to supervise them for a specific path. Invitations are stored as in-app pending requests; students approve or reject from their account.
- Supervisors can generate tasks for their assigned students using the same LLM-driven generation flow used by the platform; supervisors review and approve generated tasks before students can see them.
- Supervisors can view each assigned student's profile, task history (including latest GitHub submission link, score, and passed/failed validations), and drill into validation results per task.
- Supervisors can edit tasks they generated for their assigned students (title, scenario, task text, objectives, prerequisites, validations). Any task edited by a supervisor is flagged (`SupervisorEdited = true`) and excluded from future AI generation example pools.
- Students may also work independently without a supervisor; supervision is opt-in per path and the student can accept or reject supervisor requests.

---

## 2) User flows (step-by-step)

1. Supervisor signup: Supervisor creates account and selects a `Path` they supervise.
2. Add student: Supervisor provides a student's email and hits `Add student` → system creates a pending `SupervisorStudent` relation (Status=Pending).
3. Student sees request: Student logs in, checks `Supervisor requests` in main page, and chooses `Approve` or `Reject` (no email sent).
4. Approve: If student approves, relation becomes `Approved` and supervisor can generate tasks for that student on that path.
5. Student requests a task (optional): Student may request a new task for a skill; request appears to assigned supervisor.
6. Supervisor generates task: Supervisor uses AI generation endpoint for the student+skill; generated task is returned to supervisor.
7. Supervisor reviews/edits: Supervisor edits the task if needed and either approves it for the student or discards it.
8. Student receives approved task and completes it; evaluation stores the repository link on submission and evaluations summary.
9. Supervisor reviews history: Supervisor opens the student's profile and views tasks, validations, scores, and the last submitted repository URL for each task.

---

## 3) Data model summary (what we added/changed)

- `Supervisor` (new): { Id, FullName, Email, PasswordHash, PathId, CreatedAt }
- `SupervisorStudent` (new): { SupervisorId, StudentId, PathId, Status (Pending/Approved/Rejected), CreatedAt, ApprovedAt }
- `TaskItem.SupervisorEdited` (new boolean, default false): marks tasks edited by supervisors; these tasks must be excluded from future AI-example pools.
- Existing `TaskSubmissionEvaluation.RepositoryUrl` is used to show the student's last submitted repository for each task. We reuse it rather than adding a separate column.

All migrations were created with safe defaults (e.g., `SupervisorEdited` added with `defaultValue: false`) to avoid breaking existing rows.

---

## 4) APIs implemented (current)

- `POST /api/auth/supervisor-signup` — Supervisor signup (returns JWT with Role=Supervisor)
- `POST /api/auth/supervisor-signin` — Supervisor signin
- `GET /api/auth/supervisor-me` — Supervisor profile (requires Supervisor role)
- `POST /api/supervisor/add-student` — Supervisor adds student by email (requires Supervisor role)
- `GET /api/supervisor/my-students` — Supervisor lists their students (requires Supervisor role)
- `GET /api/auth/supervisor-requests` — Student retrieves pending supervisor requests (requires Student role)
- `POST /api/auth/supervisor-requests/{supervisorId}/{pathId}/approve` — Student approves supervisor (requires Student role)
- `POST /api/auth/supervisor-requests/{supervisorId}/{pathId}/reject` — Student rejects supervisor (requires Student role)
- Existing: `GET /api/studenttasks/by-student/{studentId}/skill/{skillId}` now includes `RepositoryUrl` in the history summaries

---

## 5) Full Implementation Checklist (detailed tasks to finish the feature)

This is an expanded copy of the plan, broken into atomic tasks with acceptance criteria. Complete them in order; many tasks are dependent on prior migrations and authorization wiring already in place.

PHASE 1 (Completed): Foundation
- [x] Create `Supervisor` and `SupervisorStudent` models and EF configurations.
- [x] Add DbSets to `MyStepDbContext` and register tables.
- [x] Create migrations for supervisor tables and `SupervisorEdited` flag (safe defaults used).
- [x] Add auth and role claims for students and supervisors; implement supervisor auth endpoints.

PHASE 2 (Completed): Basic management and approval
- [x] Implement `SupervisorService` and `SupervisorStudentRepo`.
- [x] Implement `SupervisorController` endpoints for supervisors.
- [x] Implement student endpoints to list and approve/reject supervisor requests.

PHASE 3 (Remaining): Task Request & Generation Flow
- [ ] Create `TaskGenerationRequest` model/table: { Id, StudentId, SupervisorId, PathId, MainSkillId, Status(Pending/Approved/Rejected), CreatedAt, UpdatedAt }
    - Acceptance: Requests persist, and student cannot create duplicate request for same skill if one is pending or active.
- [ ] API: `POST /api/task-generation/request` (Student) — creates request; prevents new if active task not passed.
- [ ] API: `GET /api/task-generation/requests` (Supervisor) — list pending requests for their students.
- [ ] API: `POST /api/task-generation/generate-for-student` (Supervisor) — generate task via `TaskSearchVectorService` for specific student/skill; return generated payload for review.
- [ ] API: `POST /api/task-generation/approve/{requestId}` (Supervisor) — persist generated task and assign to student; mark request Approved.

PHASE 4 (Remaining): Supervisor Task Editing & Safeguards
- [ ] API: `PUT /api/tasks/{taskId}/edit` (Supervisor) — allow editing text/title/scenario; must verify supervisor owns the student for whom task was generated.
- [ ] API: `PUT /api/tasks/{taskId}/objectives` — add/remove TaskTarget entries (only from main skill).
- [ ] API: `PUT /api/tasks/{taskId}/prerequisites` — add/remove TaskPrerequisite entries (only from other skills in the same path).
- [ ] API: `PUT /api/tasks/{taskId}/validations` — add/edit validation rules; any add of objective/prerequisite must accompany at least one validation.
- [ ] On any supervisor edit set `SupervisorEdited = true` and persist editor metadata (optional: `EditedBySupervisorId`, `EditedAt`).
- Acceptance: Supervisor cannot edit tasks they do not own; edits set the flag and are excluded from generation.

PHASE 5 (Remaining): Learning Objective Selection & Helpers
- [ ] API: `GET /api/learning-objectives/by-skill/{skillId}` — returns objectives (reuse existing endpoint) plus student's current score where available.
- [ ] API: `GET /api/learning-objectives/prerequisites-available/{mainSkillId}/{pathId}/{studentId}` — returns objectives from other skills in path filtered by student score >= 0.7.

PHASE 6 (Remaining): Exclude Supervisor-Edited Tasks From Generation
- [ ] Update all generation/search queries in `TaskSearchVectorService` and related repos to add `WHERE SupervisorEdited = false`.
- Acceptance: Supervisor-edited tasks no longer appear in example pools or similarity results used for generation.

PHASE 7 (Remaining): Supervisor Dashboard & History Enhancements
- [ ] API: `GET /api/supervisor/{supervisorId}/student/{studentId}/skill/{skillId}/history` — expands history with validation details and repository URL per task (reuse `StudentTaskService` but add supervisor checks).
- [ ] API: `GET /api/supervisor/dashboard` — returns the supervisor's students, pending requests, and recent activity summary.

PHASE 8 (Remaining): Frontend API Guide & Docs
- [ ] Update `FRONTEND_API_GUIDE.md` with all new and modified endpoints, including request/response DTOs and role requirements.

PHASE 9 (Remaining): Testing & Validation
- [ ] Unit tests for SupervisorService and repos
- [ ] Integration tests for request → generation → approval flow
- [ ] Authorization tests: ensure supervisors and students can't access each other's endpoints
- [ ] Smoke test: end-to-end scenario from supervisor signup → add student → student approve → generate task → approve task → student completes

---

## 6) Acceptance criteria (how we will know the feature is complete)

1. Supervisor accounts can sign up, sign in, and receive a JWT with `Role=Supervisor`.
2. Supervisors can add students by email and students can approve/reject requests in-app.
3. Supervisors can generate tasks for their approved students using existing LLM generation; they can review and approve tasks before students see them.
4. Supervisors can edit tasks they own; any edited task is flagged and excluded from future AI generation pools.
5. Supervisors can view per-student task history, including latest GitHub repo link and validation results.
6. Existing students and tasks continue working; the migration did not break existing rows.

---

## 7) Quick pointers for next developer / next chat

- Start with `Repository/Migrations` to see the created migrations and apply them locally.
- Use `Services/TaskSearchVectorService.cs` to find generation logic and add the `SupervisorEdited` exclusion there.
- Reuse `StudentTaskService.GetByStudentAndSkillAsync()` when implementing supervisor history endpoints.
- Update `Program.cs` DI registrations when adding new services/repos.

---

If you want, I can begin PHASE 3 (task request & generation flow) next and create the request model, repos, service methods, and controllers. Tell me to proceed and I'll start implementing the next atomic slice.
