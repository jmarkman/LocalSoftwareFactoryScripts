---
name: create-backlog-tasks
description: Generates atomic tasks from a provided prompt
disable-model-invocation: true
---

Create atomic subtasks using the Backlog CLI based on the contents of the provided prompt.

The prompt will contain one or more features with implementation details.

Each task must be sufficiently siloed such that a developer or agent is generally aware of work completed prior but unconcerned with what needs to be implemented in the future, only with what the task requires. Prefer creating small to mid-size tasks to support this.

If a given part of the prompt indicates that some of its implementation details may need review, engage in a question-and-answer dialog with the user to resolve those details.

Use the `backlog instructions` commands from `copilot-instructions` for backlog-specific information.

Relate subtasks to tasks. Avoid nesting subtasks under subtasks.

Although inefficient, avoid creating tasks in parallel to ensure that feature order and task numbers are in alignment.