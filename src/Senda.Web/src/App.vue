<script setup>
import { RouterView } from 'vue-router';
import Select from 'primevue/select';
import { useTenantStore } from './stores/tenant';

const tenantStore = useTenantStore();
</script>

<template>
  <div class="min-h-screen bg-surface-900 text-surface-0 flex flex-col">
    <header class="p-4 border-b border-surface-700 flex justify-between items-center bg-surface-950">
      <div class="flex items-center gap-8">
        <div class="flex items-center gap-2">
            <i class="pi pi-sparkles text-primary text-2xl"></i>
            <h1 class="text-xl font-bold tracking-tight">Senda <span class="text-primary">AI</span></h1>
        </div>
        <nav class="flex gap-4">
            <router-link to="/" class="text-sm font-medium hover:text-primary transition-colors" active-class="text-primary">Dashboard</router-link>
            <router-link to="/chat" class="text-sm font-medium hover:text-primary transition-colors" active-class="text-primary">Chat AI</router-link>
        </nav>
      </div>
      <div class="flex items-center gap-3">
        <Select 
            v-model="tenantStore.currentTenantId" 
            :options="tenantStore.tenants" 
            optionLabel="name" 
            optionValue="id" 
            placeholder="Seleccionar Empresa" 
            class="w-48 p-inputtext-sm"
        />
        <div class="w-8 h-8 rounded-full bg-surface-800 flex items-center justify-center text-xs font-bold text-surface-400 border border-surface-700">
            AD
        </div>
      </div>
    </header>
    
    <main class="flex-1 overflow-hidden">
      <RouterView />
    </main>
  </div>
</template>

<style>
/* PrimeVue overrides or specific global styles if any */
.p-primary {
  color: var(--p-primary-color);
}
</style>
