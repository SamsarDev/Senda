---
name: vue-prime-tailwind
description: Development standards and tools for Vue 3 + PrimeVue + Tailwind CSS
---

# Vue 3 + PrimeVue + Tailwind Skill

This skill provides the standards and tools for building the **Senda AI Concierge** frontend using the approved tech stack.

## Tech Stack Standards

1.  **Framework**: Vue 3 with Composition API (`<script setup>`).
2.  **Build Tool**: Vite.
3.  **UI Library**: [PrimeVue v4+](https://primevue.org/).
    -   **Mode**: Styled Mode with `tailwindcss-primeui` plugin.
    -   **Theme**: Noir (Sleek/Modern dark theme).
3.  **Styling**: Tailwind CSS v3/v4 for layout and responsiveness.
4.  **State Management**: Pinia (if needed for complex state).
5.  **Icons**: PrimeIcons or Lucide Vue Next.

## Project Structure (src/Senda.Web)

```
src/Senda.Web/
├── public/
├── src/
│   ├── api/            # Axios/Fetch clients for .NET API
│   ├── assets/         # Global styles/images
│   ├── components/     # Reusable UI components
│   ├── composables/    # Shared logic (useAuth, useChat)
│   ├── layouts/        # Page layouts (Default, Admin)
│   ├── router/         # Vue Router configuration
│   ├── stores/         # Pinia stores
│   ├── views/          # Page components
│   ├── App.vue         # Root component
│   └── main.js         # Entry point
├── tailwind.config.js
├── vite.config.js
└── package.json
```

## Implementation Workflow

1.  **Initialization**: Use `npx create-vite@latest ./ --template vue`.
2.  **Dependencies**: Install `primevue`, `@primevue/themes`, `tailwindcss`, `postcss`, `autoprefixer`, `lucide-vue-next`.
3.  **Tailwind Configuration**:
    -   Configure `content` to include PrimeVue files if necessary.
    -   Use `tailwindcss-primeui` plugin to sync PrimeVue tokens with Tailwind.
4.  **PrimeVue Setup**:
    -   Use `Noir` theme for a premium look.
    -   Enable `ripple` and `inputStyle: 'filled'`.
5.  **Component Standards**:
    -   Use PrimeVue components for complex UI (Datatables, Dialogs, Menus).
    -   Use Tailwind for spacing, layout (Flex/Grid), and micro-adjustments.
    -   Prefix custom components with `S` (e.g., `SChatBox.vue`).

## Best Practices

-   **Accessibility**: Leverage PrimeVue's built-in ARIA support.
-   **Performance**: Use Vite's code splitting; keep components focused.
-   **Security**: Always include `X-Tenant-Id` header in API requests via an Axios interceptor.
-   **Design**: Prioritize "Premium Aesthetics" as per Antigravity rules (vibrant colors against dark surfaces, smooth transitions).
