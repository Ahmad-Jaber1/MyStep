@baseUrl = http://68.221.175.88:5000
# MyStep Frontend API Guide

This comprehensive document covers all APIs needed for both **Student** and **Supervisor** frontends.

## API Overview

The platform has two main user types with separate authentication flows:
- **Students**: Sign up, browse paths, choose a path, complete welcome assessment, generate tasks, submit work
- **Supervisors**: Sign up, add students, generate tasks for students, edit tasks, view student history

### Flows Covered

**Student Flows:**
- student sign up / sign in
- browse paths, skills, and learning objectives
- choose a path
- complete the one-time welcome assessment
- check whether the welcome assessment is still required
- handle supervisor requests (approve/reject)
- generate a programming task
- evaluate a student's task submission
- view task history and detailed feedback

**Supervisor Flows:**
- supervisor sign up / sign in
- add students to supervise
- approve/generate tasks for students
- edit tasks (objectives, prerequisites, validations)
- view student task history
- review student submissions and feedback

## General Rules

- Base URL: use your backend host, for example `https://localhost:<port>`.
- Most endpoints return JSON.
- Protected endpoints require `Authorization: Bearer <token>`.
- On success, controllers usually return `200 OK` with the response body.
- Create endpoints return `201 Created` when successful.
- On validation errors, backend usually returns `400 Bad Request` with a plain error message.
- If a resource is missing, backend usually returns `404 Not Found` with a plain error message.
- Certain endpoints like task generation with supervisors may return `202 Accepted` to indicate async processing.

## Authentication APIs

### 1) Student Sign Up

`POST /api/auth/signup`

Use this when a new student creates an account.

Request body:
```json
{
  "fullName": "Sara Ali",
  "email": "sara@example.com",
  "password": "StrongPass123"
}
```

Request DTO:
- `fullName`: string, required
- `email`: string, required
- `password`: string, required

Success response: `StudentResponseDto`
```json
{
  "id": "b8df4e5a-8e2d-4d2d-9f8c-1dc7b1e4f111",
  "fullName": "Sara Ali",
  "email": "sara@example.com",
  "selectedPathId": null,
  "requiresWelcomeAssessment": true,
  "createdAt": "2026-04-18T10:20:30Z"
}
```

Response fields:
- `id`: guid
- `fullName`: string
- `email`: string
- `selectedPathId`: integer or null
- `requiresWelcomeAssessment`: boolean
- `createdAt`: datetime

### 2) Student Sign In

`POST /api/auth/signin`

Use this after the student logs in.

Request body:
```json
{
  "email": "sara@example.com",
  "password": "StrongPass123"
}
```

Request DTO:
- `email`: string, required
- `password`: string, required

Success response: `AuthResponseDto`
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAtUtc": "2026-04-18T12:20:30Z",
  "student": {
    "id": "b8df4e5a-8e2d-4d2d-9f8c-1dc7b1e4f111",
    "fullName": "Sara Ali",
    "email": "sara@example.com",
    "selectedPathId": null,
    "requiresWelcomeAssessment": true,
    "createdAt": "2026-04-18T10:20:30Z"
  }
}
```

Response fields:
- `token`: JWT token for protected requests
- `expiresAtUtc`: datetime
- `student`: `StudentResponseDto`

### 3) Current Student

`GET /api/auth/me`

Use this if the frontend wants to refresh the logged-in student state.

Headers:
```http
Authorization: Bearer <token>
```

Success response: `CurrentStudentDto`
```json
{
  "id": "b8df4e5a-8e2d-4d2d-9f8c-1dc7b1e4f111",
  "fullName": "Sara Ali",
  "email": "sara@example.com",
  "requiresWelcomeAssessment": true
}
```

Response fields:
- `id`: guid
- `fullName`: string
- `email`: string
- `requiresWelcomeAssessment`: boolean

### 4) Student Sign Out

`POST /api/auth/signout`

Use this when a student logs out.

Headers:
```http
Authorization: Bearer <token>
```

Success response:
```json
{
  "success": true
}
```

### 5) Supervisor Sign Up

`POST /api/auth/supervisor-signup`

Use this when a new supervisor creates an account. Supervisors supervise a specific learning path.

Request body:
```json
{
  "fullName": "Dr. Johnson",
  "email": "johnson@example.com",
  "password": "StrongPass123",
  "pathId": 2
}
```

Request DTO:
- `fullName`: string, required
- `email`: string, required
- `password`: string, required
- `pathId`: integer, required

Success response: `SupervisorResponseDto`
```json
{
  "id": "c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f",
  "fullName": "Dr. Johnson",
  "email": "johnson@example.com",
  "pathId": 2,
  "createdAt": "2026-04-18T10:20:30Z"
}
```

Response fields:
- `id`: guid
- `fullName`: string
- `email`: string
- `pathId`: integer (the path this supervisor is responsible for)
- `createdAt`: datetime

### 6) Supervisor Sign In

`POST /api/auth/supervisor-signin`

Use this after a supervisor logs in.

Request body:
```json
{
  "email": "johnson@example.com",
  "password": "StrongPass123"
}
```

Request DTO:
- `email`: string, required
- `password`: string, required

Success response: `AuthResponseDto`
```json
{
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "expiresAtUtc": "2026-04-18T12:20:30Z",
  "supervisor": {
    "id": "c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f",
    "fullName": "Dr. Johnson",
    "email": "johnson@example.com",
    "pathId": 2,
    "createdAt": "2026-04-18T10:20:30Z"
  }
}
```

Response fields:
- `token`: JWT token for protected requests
- `expiresAtUtc`: datetime
- `supervisor`: `SupervisorResponseDto`

### 7) Current Supervisor

`GET /api/auth/supervisor-me`

Use this to get the currently logged-in supervisor's profile.

Headers:
```http
Authorization: Bearer <token>
```

Success response: `SupervisorResponseDto`
```json
{
  "id": "c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f",
  "fullName": "Dr. Johnson",
  "email": "johnson@example.com",
  "pathId": 2,
  "createdAt": "2026-04-18T10:20:30Z"
}
```

Response fields:
- `id`: guid
- `fullName`: string
- `email`: string
- `pathId`: integer
- `createdAt`: datetime

## Paths, Skills, and Learning Objectives APIs

These endpoints allow both students and supervisors to browse available paths, skills, and learning objectives. All are open (no authorization required).

### Get All Paths

`GET /api/paths`

Retrieve all available learning paths.

Success response: array of `PathResponseDto`
```json
[
  {
    "id": 1,
    "name": "ASP.NET Core Backend Development",
    "description": "Learn to build robust backend APIs with ASP.NET Core"
  },
  {
    "id": 2,
    "name": "Cloud Architecture",
    "description": "Design and implement scalable cloud solutions"
  }
]
```

Response fields for each object:
- `id`: integer
- `name`: string
- `description`: string

### Get Path by ID

`GET /api/paths/{id}`

Retrieve a specific learning path.

Path parameters:
- `id`: integer, required

Success response: `PathResponseDto`
```json
{
  "id": 1,
  "name": "ASP.NET Core Backend Development",
  "description": "Learn to build robust backend APIs with ASP.NET Core"
}
```

### Get Skills by Path

`GET /api/skills/by-path/{pathId}`

Retrieve all skills associated with a learning path.

Path parameters:
- `pathId`: integer, required

Success response: array of `SkillResponseDto`
```json
[
  {
    "id": 1,
    "name": "Basic C# Programming",
    "description": "Fundamentals of C# language and syntax"
  },
  {
    "id": 4,
    "name": "ASP.NET Core Middleware",
    "description": "Understanding middleware pipeline and custom middleware"
  }
]
```

Response fields for each object:
- `id`: integer
- `name`: string
- `description`: string

### Get Learning Objectives by Skill

`GET /api/learningobjectives/by-skill/{skillId}`

Retrieve all learning objectives for a specific skill. Use this to populate the welcome assessment form or to show objective details.

Path parameters:
- `skillId`: integer, required

Success response: array of `LearningObjectiveResponseDto`
```json
[
  {
    "id": 48,
    "skillId": 4,
    "name": "Implement Custom Middleware",
    "description": "Write and register custom middleware in ASP.NET Core pipeline"
  },
  {
    "id": 50,
    "skillId": 4,
    "name": "Middleware Error Handling",
    "description": "Handle errors and exceptions in middleware"
  }
]
```

Response fields for each object:
- `id`: integer
- `skillId`: integer
- `name`: string
- `description`: string

## Student-Specific APIs

### Choose Path

`PUT /api/students/{id}`

Update a student's selected path. After sign-up, students must choose a path before task generation is available.

Headers:
```http
Authorization: Bearer <token>
```

Path parameters:
- `id`: guid, required (student ID)

Request body:
```json
{
  "selectedPathId": 1
}
```

Request DTO:
- `selectedPathId`: integer, required

Success response: `StudentResponseDto`
```json
{
  "id": "b8df4e5a-8e2d-4d2d-9f8c-1dc7b1e4f111",
  "fullName": "Sara Ali",
  "email": "sara@example.com",
  "selectedPathId": 1,
  "requiresWelcomeAssessment": true,
  "createdAt": "2026-04-18T10:20:30Z"
}
```

### Submit Welcome Assessment

`POST /api/auth/welcome-assessment`

Submit the student's initial learning objective ratings. This completes the welcome assessment and allows task generation to begin.

Headers:
```http
Authorization: Bearer <token>
Content-Type: application/json
```

Request body:
```json
{
  "ratings": [
    {
      "learningObjectiveId": 48,
      "rating": 2
    },
    {
      "learningObjectiveId": 50,
      "rating": 1
    }
  ]
}
```

Request DTO:
- `ratings`: array of rating objects, required
  - `learningObjectiveId`: integer, required
  - `rating`: number (0-4), required

Success response: `WelcomeAssessmentResponseDto`
```json
{
  "success": true,
  "message": "Welcome assessment completed successfully"
}
```

### Get Student's Supervisor Requests

`GET /api/auth/supervisor-requests`

Retrieve pending supervisor requests for the currently logged-in student. Students see this list to approve or reject supervisor invitations.

Headers:
```http
Authorization: Bearer <token>
```

Success response: array of `SupervisorRequestDto`
```json
[
  {
    "supervisorId": "c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f",
    "supervisorName": "Dr. Johnson",
    "pathId": 1,
    "pathName": "ASP.NET Core Backend Development",
    "status": "Pending",
    "createdAt": "2026-04-20T10:00:00Z"
  }
]
```

Response fields for each object:
- `supervisorId`: guid
- `supervisorName`: string
- `pathId`: integer
- `pathName`: string
- `status`: string (Pending, Approved, or Rejected)
- `createdAt`: datetime

### Approve Supervisor Request

`POST /api/auth/supervisor-requests/{supervisorId}/{pathId}/approve`

Student approves a pending supervisor request. After approval, the supervisor can generate tasks for this student on this path.

Headers:
```http
Authorization: Bearer <token>
```

Path parameters:
- `supervisorId`: guid, required
- `pathId`: integer, required

Success response: `SupervisorRequestDto`
```json
{
  "supervisorId": "c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f",
  "supervisorName": "Dr. Johnson",
  "pathId": 1,
  "pathName": "ASP.NET Core Backend Development",
  "status": "Approved",
  "approvedAt": "2026-04-20T10:05:00Z"
}
```

### Reject Supervisor Request

`POST /api/auth/supervisor-requests/{supervisorId}/{pathId}/reject`

Student rejects a pending supervisor request.

Headers:
```http
Authorization: Bearer <token>
```

Path parameters:
- `supervisorId`: guid, required
- `pathId`: integer, required

Success response: `SupervisorRequestDto`
```json
{
  "supervisorId": "c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f",
  "supervisorName": "Dr. Johnson",
  "pathId": 1,
  "pathName": "ASP.NET Core Backend Development",
  "status": "Rejected",
  "rejectedAt": "2026-04-20T10:05:00Z"
}
```

## Task Generation and Management APIs

### Task Generation - Student Mode

`POST /api/task-generation/generate`

Use this when the frontend wants the backend to generate a task for the student.

**Important**: If the student has an approved supervisor, this returns `202 Accepted` with a pending task generation request instead of generating immediately. The supervisor must then approve the task.

Headers:
```http
Authorization: Bearer <token>
Content-Type: application/json
```

Request body:
```json
{
  "studentId": "d546ab00-024d-42b4-837c-9d42da4fa281",
  "mainSkillId": 4
}
```

Request DTO:
- `studentId`: guid, required
- `mainSkillId`: integer, required

**Case 1: No Supervisor (200 OK)** - Task is generated and persisted immediately
```json
{
  "taskId": "f6f3b2a6-0df6-4b6b-8f4d-7d4a2b5f4c1a",
  "taskData": {
    "task_name": "Request Audit Middleware with Header Injection and Attribute Routing",
    "skill_category": "ASP.NET Core Logics",
    "scenario": {
      "story": "A fintech startup requires an internal auditing mechanism for their Quote Calculation API.",
      "requirement": "Implement a single feature with middleware and an attribute-routed controller endpoint."
    },
    "targeted_objectives": [48, 50, 51],
    "additional_skills_required": [
      {
        "skill_id": 1,
        "skill_name": "Basic C# Programming",
        "used_learning_goal": 1,
        "justification": "Required to declare timestamps and generate unique identifiers."
      }
    ],
    "instructions": [
      "Start by defining the data model with annotations.",
      "Add the middleware and register it in the pipeline."
    ],
    "validation_criteria": [
      {
        "skill_id": 4,
        "criterion": "A custom middleware class is implemented and registered in the application pipeline.",
        "related_learning_objective": 48
      }
    ],
    "hints": [
      "Start with the data model.",
      "Then implement the middleware behavior."
    ]
  }
}
```

**Case 2: With Supervisor (202 Accepted)** - Request is created for supervisor approval
```json
{
  "mode": "supervisor_request",
  "message": "Task generation request sent to supervisor for review.",
  "request": {
    "id": "a1b2c3d4-e5f6-4a5b-9c8d-1e2f3a4b5c6d",
    "studentId": "d546ab00-024d-42b4-837c-9d42da4fa281",
    "mainSkillId": 4,
    "status": "Pending",
    "createdAt": "2026-04-29T15:00:00Z"
  }
}
```

Important notes:
- When no supervisor: `taskId` is created and task is ready for the student.
- When supervisor exists: a request is created with status "Pending"; student cannot start work until supervisor approves.
- `validation_criteria` uses `related_learning_objective: 0` for business-logic checks not tied to a specific learning objective.
- The model output may include objective IDs; backend enforces allowed target and prerequisite lists.

### Mark Task As Passed

`POST /api/studenttasks/{studentId}/{taskId}/mark-passed`

Use this after the student completes a generated task and you want to mark it as passed.

Headers:
```http
Authorization: Bearer <token>
```

Path parameters:
- `studentId`: guid, required
- `taskId`: guid, required

Optional query parameter:
- `score`: number from `0` to `100`

Example request:
```http
POST /api/studenttasks/d546ab00-024d-42b4-837c-9d42da4fa281/f6f3b2a6-0df6-4b6b-8f4d-7d4a2b5f4c1a/mark-passed?score=85
Authorization: Bearer <token>
```

Success response: `StudentTaskResponseDto`
```json
{
  "studentId": "d546ab00-024d-42b4-837c-9d42da4fa281",
  "taskId": "f6f3b2a6-0df6-4b6b-8f4d-7d4a2b5f4c1a",
  "numberInMainSkill": 3,
  "passed": true,
  "startedAt": "2026-04-29T15:00:00Z",
  "completedAt": "2026-04-29T15:10:00Z",
  "score": 85
}
```

Response fields:
- `studentId`: guid
- `taskId`: guid
- `numberInMainSkill`: integer
- `passed`: boolean
- `startedAt`: datetime or null
- `completedAt`: datetime or null
- `score`: number or null

### Evaluate Task Submission

`POST /api/studenttasks/{studentId}/{taskId}/evaluate`

Use this when the frontend wants the backend to inspect a student's submitted repository and return a structured evaluation.

Headers:
```http
Authorization: Bearer <token>
Content-Type: application/json
```

Path parameters:
- `studentId`: guid, required
- `taskId`: guid, required

Request body:
```json
{
  "repositoryUrl": "https://github.com/example/student-submission",
  "ref": "main"
}
```

Request DTO:
- `repositoryUrl`: string, required
- `ref`: string, optional

Success response: `TaskSubmissionEvaluationResponseDto`
```json
{
  "evaluationId": "0f8b5e8a-4a1d-4d8d-9b55-9a3f4f0b7b8a",
  "studentId": "d546ab00-024d-42b4-837c-9d42da4fa281",
  "taskId": "f6f3b2a6-0df6-4b6b-8f4d-7d4a2b5f4c1a",
  "evaluatedAt": "2026-04-29T15:20:00Z",
  "overallSummary": "The solution is mostly correct, but the middleware is not registered in the pipeline.",
  "validationResults": [
    {
      "validationId": 1,
      "skillId": 4,
      "objectiveId": 48,
      "validationString": "A custom middleware class is implemented and registered in the application pipeline.",
      "isPass": false,
      "whyNotPass": "The middleware class exists, but it is never added to the request pipeline."
    }
  ],
  "studentGoodPoints": [
    "The controller endpoint follows attribute routing.",
    "The project structure is clean and easy to navigate."
  ],
  "studentWeaknesses": [
    "Pipeline registration is missing for the middleware."
  ],
  "topicsToRead": [
    "ASP.NET Core middleware registration",
    "Request pipeline ordering"
  ]
}
```

Response fields:
- `evaluationId`: guid
- `studentId`: guid
- `taskId`: guid
- `evaluatedAt`: datetime
- `overallSummary`: string
- `validationResults`: list of per-criterion results
- `studentGoodPoints`: list of strings
- `studentWeaknesses`: list of strings
- `topicsToRead`: list of strings

### Get Task History by Skill

`GET /api/studenttasks/by-student/{studentId}/skill/{skillId}`

Use this to load the history of tasks completed for a specific skill.

Headers:
```http
Authorization: Bearer <token>
```

Path parameters:
- `studentId`: guid, required
- `skillId`: integer, required

Success response: array of `TaskHistorySummaryDto`
```json
[
  {
    "taskId": "f6f3b2a6-0df6-4b6b-8f4d-7d4a2b5f4c1a",
    "taskName": "Request Audit Middleware with Header Injection",
    "numberInSkill": 1,
    "passed": true,
    "completedAt": "2026-04-29T15:10:00Z",
    "score": 85,
    "passedValidations": 3,
    "totalValidations": 4
  }
]
```

Response fields for each object:
- `taskId`: guid
- `taskName`: string (name of the task)
- `numberInSkill`: integer (which number task this is for the skill, 1-based)
- `passed`: boolean
- `completedAt`: datetime or null
- `score`: number or null (percentage: passed validations / total validations * 100)
- `passedValidations`: integer (number of validation criteria passed in the latest evaluation)
- `totalValidations`: integer (total number of validation criteria for this task)

### Get Task Details

`GET /api/studenttasks/{studentId}/{taskId}/details`

Use this to view the full details of a specific task, including validation results and feedback.

Headers:
```http
Authorization: Bearer <token>
```

Path parameters:
- `studentId`: guid, required
- `taskId`: guid, required

Success response: `TaskDetailsResponseDto`
```json
{
  "taskId": "f6f3b2a6-0df6-4b6b-8f4d-7d4a2b5f4c1a",
  "taskName": "Request Audit Middleware with Header Injection and Attribute Routing",
  "numberInSkill": 1,
  "passed": true,
  "startedAt": "2026-04-29T15:00:00Z",
  "completedAt": "2026-04-29T15:10:00Z",
  "score": 85,
  "repositoryUrl": "https://github.com/example/student-submission",
  "repositoryRef": "main",
  "evaluatedAt": "2026-04-29T15:20:00Z",
  "overallSummary": "The solution is mostly correct, but the middleware is not registered in the pipeline.",
  "validationResults": [
    {
      "validationId": 1,
      "skillId": 4,
      "objectiveId": 48,
      "validationString": "A custom middleware class is implemented and registered in the application pipeline.",
      "isPass": false,
      "whyNotPass": "The middleware class exists, but it is never added to the request pipeline."
    }
  ],
  "studentGoodPoints": [
    "The controller endpoint follows attribute routing.",
    "The project structure is clean and easy to navigate."
  ],
  "studentWeaknesses": [
    "Pipeline registration is missing for the middleware."
  ],
  "topicsToRead": [
    "ASP.NET Core middleware registration",
    "Request pipeline ordering"
  ]
}
```

Response fields:
- `taskId`: guid
- `taskName`: string
- `numberInSkill`: integer
- `passed`: boolean
- `startedAt`: datetime or null
- `completedAt`: datetime or null
- `score`: number or null (percentage of validations passed)
- `repositoryUrl`: string (the GitHub repository URL that was evaluated)
- `repositoryRef`: string or null (the branch/tag/commit that was evaluated)
- `evaluatedAt`: datetime or null (when the evaluation was performed)
- `overallSummary`: string (high-level feedback on the submission)
- `validationResults`: array of validation detail objects
  - `validationId`: integer
  - `skillId`: integer
  - `objectiveId`: integer (0 if not tied to a specific learning objective)
  - `validationString`: string (the validation criterion)
  - `isPass`: boolean
  - `whyNotPass`: string (reason for failure, empty if passed)
- `studentGoodPoints`: array of strings (things the student did well)
- `studentWeaknesses`: array of strings (areas for improvement)
- `topicsToRead`: array of strings (recommended reading/learning topics)

## Supervisor Management APIs

These endpoints are only available to users with the `Supervisor` role and require the `Authorization: Bearer <token>` header.

### Add Student to Supervision

`POST /api/supervisor/add-student`

Supervisor adds a student by email address. This creates a pending supervision request that the student must approve.

Headers:
```http
Authorization: Bearer <token>
Content-Type: application/json
```

Request body:
```json
{
  "studentEmail": "sara@example.com"
}
```

Request DTO:
- `studentEmail`: string, required

Success response: `SupervisorStudentResponseDto`
```json
{
  "supervisorId": "c3d4e5f6-a7b8-4c9d-0e1f-2a3b4c5d6e7f",
  "studentId": "b8df4e5a-8e2d-4d2d-9f8c-1dc7b1e4f111",
  "pathId": 1,
  "status": "Pending",
  "createdAt": "2026-04-20T10:00:00Z"
}
```

Response fields:
- `supervisorId`: guid
- `studentId`: guid
- `pathId`: integer (path for which supervision applies)
- `status`: string (Pending, Approved, or Rejected)
- `createdAt`: datetime

### Get My Students

`GET /api/supervisor/my-students`

Retrieve the list of students supervised by the current supervisor (approved students only).

Headers:
```http
Authorization: Bearer <token>
```

Success response: array of `StudentResponseDto`
```json
[
  {
    "id": "b8df4e5a-8e2d-4d2d-9f8c-1dc7b1e4f111",
    "fullName": "Sara Ali",
    "email": "sara@example.com",
    "selectedPathId": 1,
    "requiresWelcomeAssessment": false,
    "createdAt": "2026-04-18T10:20:30Z"
  }
]
```

### Get Student's Skill History

`GET /api/supervisor/students/{studentId}/skills/{skillId}/history`

Retrieve the task history for a specific student and skill. This allows supervisors to see all tasks completed by a student in a given skill, including scores and validation results.

Headers:
```http
Authorization: Bearer <token>
```

Path parameters:
- `studentId`: guid, required
- `skillId`: integer, required

Success response: array of `TaskHistorySummaryDto`
```json
[
  {
    "taskId": "f6f3b2a6-0df6-4b6b-8f4d-7d4a2b5f4c1a",
    "taskName": "Request Audit Middleware with Header Injection",
    "numberInSkill": 1,
    "passed": true,
    "completedAt": "2026-04-29T15:10:00Z",
    "score": 85,
    "passedValidations": 3,
    "totalValidations": 4
  }
]
```

Response fields for each object:
- `taskId`: guid
- `taskName`: string
- `numberInSkill`: integer
- `passed`: boolean
- `completedAt`: datetime or null
- `score`: number or null
- `passedValidations`: integer
- `totalValidations`: integer

## Task Generation Request APIs (Supervisor Approval Flow)

When a student with an approved supervisor requests task generation, a **task generation request** is created instead of immediately generating the task. The supervisor must then approve and persist the generated task.

### Get Pending Task Generation Requests

`GET /api/task-generation/requests`

Retrieve all pending task generation requests for the current supervisor's students.

Headers:
```http
Authorization: Bearer <token>
```

Success response: array of `TaskGenerationRequestDto`
```json
[
  {
    "id": "a1b2c3d4-e5f6-4a5b-9c8d-1e2f3a4b5c6d",
    "studentId": "b8df4e5a-8e2d-4d2d-9f8c-1dc7b1e4f111",
    "studentName": "Sara Ali",
    "mainSkillId": 4,
    "skillName": "ASP.NET Core Middleware",
    "status": "Pending",
    "createdAt": "2026-04-29T15:00:00Z"
  }
]
```

Response fields for each object:
- `id`: guid
- `studentId`: guid
- `studentName`: string
- `mainSkillId`: integer
- `skillName`: string
- `status`: string (Pending, Approved, or Rejected)
- `createdAt`: datetime

### Generate and Preview Task

`POST /api/task-generation/requests/{requestId}/approve-and-generate`

Generate a task for a pending request without persisting it yet. Allows supervisor to preview the generated content before committing.

Headers:
```http
Authorization: Bearer <token>
```

Path parameters:
- `requestId`: guid, required

Success response: Generated task JSON object
```json
{
  "taskId": "temp-preview-id",
  "taskData": {
    "task_name": "Request Audit Middleware with Header Injection and Attribute Routing",
    "skill_category": "ASP.NET Core Logics",
    "scenario": {
      "story": "A fintech startup requires an internal auditing mechanism...",
      "requirement": "Implement a single feature with middleware..."
    },
    "targeted_objectives": [48, 50, 51],
    "additional_skills_required": [],
    "instructions": ["Start by defining the data model..."],
    "validation_criteria": [
      {
        "skill_id": 4,
        "criterion": "A custom middleware class is implemented...",
        "related_learning_objective": 48
      }
    ],
    "hints": [...]
  }
}
```

Important notes:
- The `taskId` in the preview is temporary and not persisted.
- Supervisors can edit the task data before approving and persisting.
- If satisfied, call `/approve-and-persist` with the generated content.

### Approve and Persist Generated Task

`POST /api/task-generation/requests/{requestId}/approve-and-persist`

Approve a task generation request by providing the reviewed/edited task content. This persists the task in the database for the student and marks the request as approved.

Headers:
```http
Authorization: Bearer <token>
Content-Type: application/json
```

Path parameters:
- `requestId`: guid, required

Request body:
```json
{
  "generatedContent": {
    "task_name": "Request Audit Middleware...",
    "skill_category": "ASP.NET Core Logics",
    "scenario": {
      "story": "A fintech startup requires...",
      "requirement": "Implement a single feature..."
    },
    "targeted_objectives": [48, 50, 51],
    "additional_skills_required": [],
    "instructions": ["Start by..."],
    "validation_criteria": [
      {
        "skill_id": 4,
        "criterion": "A custom middleware class is implemented...",
        "related_learning_objective": 48
      }
    ],
    "hints": []
  }
}
```

Success response: `TaskResponseDto`
```json
{
  "taskId": "f6f3b2a6-0df6-4b6b-8f4d-7d4a2b5f4c1a",
  "studentId": "b8df4e5a-8e2d-4d2d-9f8c-1dc7b1e4f111",
  "mainSkillId": 4,
  "status": "Approved",
  "createdAt": "2026-04-29T15:05:00Z"
}
```

## Task Editing APIs (Supervisor Only)

Supervisors can edit tasks they have generated for their students. Any task edited by a supervisor is marked with a flag (`supervisorEdited = true`) and excluded from AI generation example pools.

### Edit Task Objectives

`PUT /api/tasks/{taskId}/objectives`

Add or remove learning objectives (targets) from a task.

Headers:
```http
Authorization: Bearer <token>
Content-Type: application/json
```

Path parameters:
- `taskId`: guid, required

Request body:
```json
{
  "targetObjectiveIds": [48, 50, 51]
}
```

Request DTO:
- `targetObjectiveIds`: array of integers (learning objective IDs to set as targets)

Success response: `TaskResponseDto`
```json
{
  "taskId": "f6f3b2a6-0df6-4b6b-8f4d-7d4a2b5f4c1a",
  "mainSkillId": 4,
  "targetObjectiveIds": [48, 50, 51],
  "supervisorEdited": true,
  "editedAt": "2026-04-30T10:00:00Z"
}
```

### Edit Task Prerequisites

`PUT /api/tasks/{taskId}/prerequisites`

Add or remove prerequisite learning objectives from other skills in the same path.

Headers:
```http
Authorization: Bearer <token>
Content-Type: application/json
```

Path parameters:
- `taskId`: guid, required

Request body:
```json
{
  "prerequisiteObjectiveIds": [1, 2, 5]
}
```

Request DTO:
- `prerequisiteObjectiveIds`: array of integers (learning objective IDs from other skills to set as prerequisites)

Success response: `TaskResponseDto`
```json
{
  "taskId": "f6f3b2a6-0df6-4b6b-8f4d-7d4a2b5f4c1a",
  "mainSkillId": 4,
  "prerequisiteObjectiveIds": [1, 2, 5],
  "supervisorEdited": true,
  "editedAt": "2026-04-30T10:00:00Z"
}
```

### Edit Task Validation Criteria

`PUT /api/tasks/{taskId}/validations`

Update the validation criteria (rubric) for a task. Each criterion checks whether the student met a specific learning objective.

Headers:
```http
Authorization: Bearer <token>
Content-Type: application/json
```

Path parameters:
- `taskId`: guid, required

Request body:
```json
{
  "validations": [
    {
      "skillId": 4,
      "objectiveId": 48,
      "criterion": "A custom middleware class is implemented and registered in the application pipeline."
    },
    {
      "skillId": 4,
      "objectiveId": 50,
      "criterion": "The middleware correctly processes HTTP requests and responses."
    }
  ]
}
```

Request DTO:
- `validations`: array of validation objects
  - `skillId`: integer (skill ID for this criterion)
  - `objectiveId`: integer (learning objective ID this criterion targets)
  - `criterion`: string (the validation criterion text)

Success response: `TaskResponseDto`
```json
{
  "taskId": "f6f3b2a6-0df6-4b6b-8f4d-7d4a2b5f4c1a",
  "mainSkillId": 4,
  "validationCount": 2,
  "supervisorEdited": true,
  "editedAt": "2026-04-30T10:00:00Z"
}
```

## Utility Endpoints

### Flatten GitHub Repository

`POST /api/github-repositories/flatten`

Use this to inspect a GitHub repository and get its flattened source code. Useful for analyzing student submissions or debugging.

Headers:
```http
Authorization: Bearer <token>
Content-Type: application/json
```

Request body:
```json
{
  "repositoryUrl": "https://github.com/example/student-submission",
  "ref": "main"
}
```

Request DTO:
- `repositoryUrl`: string, required
- `ref`: string, optional (branch, tag, or commit SHA)

Success response: `FlattenedRepositoryResponseDto`
```json
{
  "flattened_source_code": "// Combined source from all files...",
  "files": [
    {
      "path": "src/Middleware.cs",
      "language": "csharp"
    }
  ]
}
```

Response fields:
- `flattened_source_code`: string (all source files combined into one)
- `files`: array of file objects
  - `path`: string (file path in repository)
  - `language`: string (detected language)

## Important Frontend Flows

### Student Frontend Flow

1. Student signs up via `POST /api/auth/signup`.
2. Student signs in via `POST /api/auth/signin` and receives JWT token.
3. Frontend checks `requiresWelcomeAssessment` from sign-in response.
4. Student chooses a path via `PUT /api/students/{id}`.
5. Frontend loads skills via `GET /api/skills/by-path/{pathId}`.
6. Frontend loads learning objectives via `GET /api/learningobjectives/by-skill/{skillId}`.
7. Frontend shows welcome form, split into pages by skill.
8. Student rates each objective (0-4) and submits via `POST /api/auth/welcome-assessment`.
9. Frontend checks for supervisor requests via `GET /api/auth/supervisor-requests`.
10. If supervisor request exists, student can approve/reject.
11. To generate a task, call `POST /api/task-generation/generate`.
    - If no supervisor: task is generated immediately (200 OK).
    - If supervisor exists: request is created (202 Accepted); student waits for supervisor approval.
12. Once task is available, student completes it and submits via `POST /api/studenttasks/{studentId}/{taskId}/evaluate` with GitHub URL.
13. Student can view history via `GET /api/studenttasks/by-student/{studentId}/skill/{skillId}`.
14. Student can view detailed feedback via `GET /api/studenttasks/{studentId}/{taskId}/details`.

### Supervisor Frontend Flow

1. Supervisor signs up via `POST /api/auth/supervisor-signup` and selects a path.
2. Supervisor signs in via `POST /api/auth/supervisor-signin` and receives JWT token.
3. Supervisor can add students via `POST /api/supervisor/add-student` (by email).
4. Supervisor retrieves approved students via `GET /api/supervisor/my-students`.
5. Supervisor checks for pending task generation requests via `GET /api/task-generation/requests`.
6. For each request, supervisor can:
   - Preview the generated task via `POST /api/task-generation/requests/{requestId}/approve-and-generate`.
   - Edit the task content if needed.
   - Persist the task via `POST /api/task-generation/requests/{requestId}/approve-and-persist`.
7. Supervisor can view student history via `GET /api/supervisor/students/{studentId}/skills/{skillId}/history`.
8. Supervisor can edit tasks via `PUT /api/tasks/{taskId}/objectives`, `/prerequisites`, or `/validations`.
9. Supervisor can view detailed submission feedback via `GET /api/studenttasks/{studentId}/{taskId}/details`.

## Error Handling

All endpoints follow these error patterns:

- **400 Bad Request**: Validation error or invalid input. Response includes a plain text error message.
- **404 Not Found**: Resource not found. Response includes a plain text message indicating which resource is missing.
- **401 Unauthorized**: Missing or invalid authentication token.
- **403 Forbidden**: Authenticated but not authorized for this resource (e.g., wrong role).
- **202 Accepted**: Async operation initiated (e.g., task generation request created; student must wait for supervisor).

## Rate Limiting & Performance Notes

- No explicit rate limiting is currently implemented.
- Task generation can take 10-30 seconds depending on model load.
- GitHub repository flattening can take 5-10 seconds for large repositories.
- Frontend should show loading indicators during these operations.

## Version History

- **v1.0** (2026-04-18): Initial API guide for student flows.
- **v2.0** (2026-04-30): Added comprehensive supervisor APIs, task editing, and request management flows.
