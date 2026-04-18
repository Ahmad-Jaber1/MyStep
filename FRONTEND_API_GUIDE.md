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
