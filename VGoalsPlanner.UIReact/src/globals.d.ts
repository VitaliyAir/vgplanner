// Значения подставляет vite.config.ts (define) при сборке:
//   __APP_VERSION__ — из VGoalsPlanner.UIReact/version.txt рядом с vite.config.ts;
//   __APP_COMMIT__  — из Docker build-arg (в контексте сборки нет .git).
declare const __APP_VERSION__: string
declare const __APP_COMMIT__: string
