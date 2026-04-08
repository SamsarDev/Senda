import { defineStore } from 'pinia';
import { ref, watch } from 'vue';

export const useTenantStore = defineStore('tenant', () => {
  const currentTenantId = ref(localStorage.getItem('senda_tenant_id') || null);
  const tenants = ref([]);

  watch(currentTenantId, (newId) => {
    if (newId) {
      localStorage.setItem('senda_tenant_id', newId);
    } else {
      localStorage.removeItem('senda_tenant_id');
    }
  });

  function setTenant(id) {
    currentTenantId.value = id;
  }

  function setTenants(data) {
    tenants.value = data;
    // Auto-select first tenant if none selected
    if (!currentTenantId.value && data.length > 0) {
      currentTenantId.value = data[0].id;
    }
  }

  return {
    currentTenantId,
    tenants,
    setTenant,
    setTenants
  };
});
