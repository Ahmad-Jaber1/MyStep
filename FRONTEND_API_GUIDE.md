@baseUrl = http://68.221.175.88:5000
# Frontend API Guide

This document is for the frontend team. It only includes the APIs needed for the current flow:

- student sign up
- student sign in
- browse paths, skills, and learning objectives
- choose a path
- complete the one-time welcome assessment
- check whether the welcome assessment is still required

## General Rules

- Base URL: use your backend host, for example `https://localhost:<port>`.
- Most endpoints return JSON.
- Protected endpoints require `Authorization: Bearer <token>`.
- On success, controllers usually return `200 OK` with the response body.
- Create endpoints return `201 Created` when successful.
- On validation errors, backend usually returns `400 Bad Request` with a plain error message.
- If a resource is missing, backend usually returns `404 Not Found` with a plain error message.

## Important Frontend Flow

1. Student signs up.
2. Student signs in.
3. Frontend checks `requiresWelcomeAssessment` from sign-in response, or calls `GET /api/auth/me`.
4. Student chooses a path.
5. Frontend loads skills for that path, then learning objectives for each skill.
6. Frontend shows the welcome form, split into pages by skill.
7. Student rates each learning objective from `0` to `4`.
8. Frontend submits all ratings once.
9. Backend marks welcome assessment as completed, so it will not appear again for that student.

## Task Generation API

This flow is protected and should be called only after the student logs in.
The frontend only needs the generate endpoint. The prepare endpoint is an internal backend step and is not needed by the UI.

### 7) Generate Task

`POST /api/task-generation/generate`

Use this when the frontend wants the backend to prepare the student-specific prompt and return one generated task as pure JSON.

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

Success response: pure task JSON object returned directly from the model output
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
      },
      {
        "skill_id": 0,
        "criterion": "A request with invalid quote quantity returns a 400 status code.",
        "related_learning_objective": 0
      }
    ],
    "hints": [
      "Start with the data model.",
      "Then implement the middleware behavior."
    ]
  }
}
```

Important notes:
- The response now includes a wrapper object with `taskId` and `taskData`.
- `taskId` is the newly created task identifier.
- `taskData` is the generated task JSON returned by the model.
- The frontend can store the task id immediately and render `taskData` directly or transform it into the UI format it needs.
- `validation_criteria` uses `skill_id: 0` and `related_learning_objective: 0` for business-logic checks that are not tied to a specific learning objective.
- The model output may include objective IDs that do not belong to the current student unless the backend has already constrained them in the prompt; the backend is responsible for enforcing the allowed target and prerequisite lists.
- If the frontend needs to regenerate a task, call the same endpoint again with the same `studentId` and `mainSkillId`.

### 8) Mark Task As Passed

`POST /api/StudentTasks/{studentId}/{taskId}/mark-passed`

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
POST /api/StudentTasks/d546ab00-024d-42b4-837c-9d42da4fa281/f6f3b2a6-0df6-4b6b-8f4d-7d4a2b5f4c1a/mark-passed?score=85
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

## Auth APIs

### 1) Sign Up

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

### 2) Sign In

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

## Path Browsing APIs

### 4) Get All Paths

`GET /api/paths`

Use this to show the list of paths before the student chooses one.

Success response: array of `PathItemResponseDto`
```json
[
  {
    "id": 1,
    "name": "Backend Development",
    "description": "Learn backend basics and advanced topics"
  },
  {
    "id": 2,
    "name": "Frontend Development",
    "description": "Learn UI and client-side development"
  }
]
```

Each path object:
- `id`: integer
- `name`: string
- `description`: string or null

### 5) Get Skills by Path

`GET /api/skills/by-path/{pathId}`

Use this after the student selects a path, or before the welcome form if you want to show the path structure.

Path parameter:
- `pathId`: integer

Success response: array of `SkillResponseDto`
```json
[
  {
    "id": 10,
    "pathId": 1,
    "name": "C# Basics",
    "description": "Core language concepts"
  },
  {
    "id": 11,
    "pathId": 1,
    "name": "ASP.NET Core",
    "description": "Build web APIs and web apps"
  }
]
```

Each skill object:
- `id`: integer
- `pathId`: integer
- `name`: string
- `description`: string or null

### 6) Get Learning Objectives by Skill

`GET /api/learningobjectives/by-skill/{skillId}`

Use this to build each welcome-form page for a skill.

Path parameter:
- `skillId`: integer

Success response: array of `LearningObjectiveResponseDto`
```json
[
  {
    "id": 101,
    "skillId": 10,
    "description": "Understand variables and types"
  },
  {
    "id": 102,
    "skillId": 10,
    "description": "Use loops and conditions"
  }
]
```

Each learning objective object:
- `id`: integer
- `skillId`: integer
- `description`: string

### 6.1) Get Path by Id

`GET /api/paths/{id}`

Use this when the dashboard opens a specific path details page.

Path parameter:
- `id`: integer

Success response: `PathItemResponseDto`
```json
{
  "id": 1,
  "name": "Backend Development",
  "description": "Learn backend basics and advanced topics"
}
```

### 6.2) Get All Skills

`GET /api/skills`

Use this for admin/dashboard screens that show all skills across all paths.

Success response: array of `SkillResponseDto`

### 6.3) Get Skill by Id

`GET /api/skills/{id}`

Use this for skill details pages.

Path parameter:
- `id`: integer

Success response: `SkillResponseDto`

### 6.4) Get All Learning Objectives

`GET /api/learningobjectives`

Use this for admin/dashboard views that need a full catalog of objectives.

Success response: array of `LearningObjectiveResponseDto`

### 6.5) Get Learning Objective by Id

`GET /api/learningobjectives/{id}`

Use this for a single learning objective details screen.

Path parameter:
- `id`: integer

Success response: `LearningObjectiveResponseDto`

### 6.6) Dashboard Progress APIs (Student Learning Objectives)

These endpoints are useful for student-progress widgets and analytics cards in the dashboard.
All endpoints below require:
```http
Authorization: Bearer <token>
```

`GET /api/studentlearningobjectives`
- Get all student-learning-objective records.

`GET /api/studentlearningobjectives/{studentId}/{learningObjectiveId}`
- Get one student-learning-objective record by composite key.

`GET /api/studentlearningobjectives/by-student/{studentId}`
- Get all learning-objective records for one student (very useful for progress dashboard).

`GET /api/studentlearningobjectives/by-learning-objective/{learningObjectiveId}`
- Get all students' records for one learning objective.

### 6.7) Important Availability Note

There is currently no direct endpoint:
- `GET /api/learningobjectives/by-path/{pathId}`

To load learning objectives for a path, use this sequence:
1. `GET /api/skills/by-path/{pathId}`
2. For each returned skill, call `GET /api/learningobjectives/by-skill/{skillId}`

## Choose Path

### 7) Select Path (Recommended)

`PUT /api/auth/selected-path`

Use this when the student chooses the path they want to learn.

Headers:
```http
Authorization: Bearer <token>
```

Request body:
```json
{
  "selectedPathId": 1
}
```

Request DTO: `SelectPathDto`
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

## Welcome Assessment

### 8) Submit Welcome Assessment

`POST /api/auth/welcome-assessment`

Use this once after the student finishes all welcome pages.

Headers:
```http
Authorization: Bearer <token>
```

Request body:
```json
{
  "objectives": [
    { "learningObjectiveId": 101, "score": 0 },
    { "learningObjectiveId": 102, "score": 3 },
    { "learningObjectiveId": 201, "score": 4 }
  ]
}
```

Request DTO: `SubmitWelcomeAssessmentDto`
- `objectives`: array of `WelcomeAssessmentItemDto`

Each object in `objectives`:
- `learningObjectiveId`: integer, required
- `score`: integer, required, must be one of `0`, `1`, `2`, `3`, `4`

Frontend input meaning:
- `0` = knows nothing
- `1` = low knowledge
- `2` = medium-low knowledge
- `3` = good knowledge
- `4` = very strong knowledge

Backend stored score mapping:
- `0 -> 0.0`
- `1 -> 0.2`
- `2 -> 0.4`
- `3 -> 0.6`
- `4 -> 0.65`

Success response:
```json
{
  "success": true
}
```

Important behavior:
- If the student already completed the welcome assessment, backend returns an error and does not allow resubmission.
- After a successful submit, `requiresWelcomeAssessment` becomes `false`.

## Data You Should Keep in Frontend State

After sign in, keep these values:
- `token`
- `student.id`
- `student.selectedPathId`
- `student.requiresWelcomeAssessment`

Recommended flow:
1. Sign in.
2. If `requiresWelcomeAssessment` is `true`, send the student to the path selection + welcome assessment flow.
3. Load `GET /api/paths`.
4. After choosing a path, call `PUT /api/auth/selected-path`.
5. Load `GET /api/skills/by-path/{pathId}`.
6. For each skill, load `GET /api/learningobjectives/by-skill/{skillId}`.
7. Render one page per skill.
8. Submit all ratings once with `POST /api/auth/welcome-assessment`.
9. Use `GET /api/auth/me` or the sign-in response to confirm `requiresWelcomeAssessment` is now `false`.

## Notes For Frontend

- The welcome form is one-time only per student.
- The backend decides if the form should still appear using the `requiresWelcomeAssessment` flag.
- The frontend should not try to calculate or store the backend score values directly. It should send only the `0..4` rating.
- `skills` and `learning objectives` are separate API calls, so build the welcome pages dynamically from them.
