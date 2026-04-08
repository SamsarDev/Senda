<script setup>
import { ref, onMounted } from 'vue';
import { useTenantStore } from '../stores/tenant';
import apiClient from '../api/client';
import { Upload, FileText, CheckCircle, Loader2, Plus, Building } from 'lucide-vue-next';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Select from 'primevue/select';
import Card from 'primevue/card';
import DataTable from 'primevue/datatable';
import Column from 'primevue/column';
import Message from 'primevue/message';

const tenantStore = useTenantStore();
const documents = ref([]);
const loading = ref(false);
const uploading = ref(false);
const newTenantName = ref('');
const showTenantDialog = ref(false);

const loadTenants = async () => {
    try {
        const response = await apiClient.get('/Tenants');
        tenantStore.setTenants(response.data);
    } catch (err) {
        console.error('Error loading tenants', err);
    }
};

const createTenant = async () => {
    if (!newTenantName.value) return;
    try {
        await apiClient.post(`/Tenants?name=${newTenantName.value}`);
        newTenantName.value = '';
        showTenantDialog.value = false;
        await loadTenants();
    } catch (err) {
        console.error('Error creating tenant', err);
    }
};

const loadDocuments = async () => {
    if (!tenantStore.currentTenantId) return;
    loading.value = true;
    try {
        // We'll need to implement this endpoint or just mock it for now
        // Based on KnowledgeController, we only have 'upload'
        // Let's assume there is a GET /api/Knowledge/documents
        // response = await apiClient.get('/Knowledge/documents');
        // documents.value = response.data;
        documents.value = []; // Placeholder
    } catch (err) {
        console.error('Error loading documents', err);
    } finally {
        loading.value = false;
    }
};

const onFileUpload = async (event) => {
    const file = event.target.files[0];
    if (!file) return;

    const formData = new FormData();
    formData.append('file', file);

    uploading.value = true;
    try {
        await apiClient.post('/Knowledge/upload', formData, {
            headers: { 'Content-Type': 'multipart/form-data' }
        });
        await loadDocuments();
    } catch (err) {
        console.error('Upload failed', err);
    } finally {
        uploading.value = false;
        event.target.value = ''; // Reset input
    }
};

onMounted(async () => {
    await loadTenants();
    await loadDocuments();
});
</script>

<template>
  <div class="p-6 max-w-7xl mx-auto space-y-6">
    <div class="flex justify-between items-center">
      <div>
        <h1 class="text-3xl font-extrabold text-white">Gestión de <span class="text-primary">Conocimiento</span></h1>
        <p class="text-surface-400">Administra los documentos que alimentan a tu IA.</p>
      </div>
      <div class="flex gap-3">
        <Select 
            v-model="tenantStore.currentTenantId" 
            :options="tenantStore.tenants" 
            optionLabel="name" 
            optionValue="id" 
            placeholder="Seleccionar Empresa" 
            class="w-64"
            @change="loadDocuments"
        />
        <Button icon="pi pi-plus" label="Nueva Empresa" severity="secondary" @click="showTenantDialog = true" />
      </div>
    </div>

    <!-- Create Tenant Section if empty -->
    <div v-if="tenantStore.tenants.length === 0" class="flex justify-center py-12">
        <Card class="max-w-md w-full text-center">
            <template #title>Bienvenido a Senda</template>
            <template #content>
                <p class="mb-4">Para comenzar, crea tu primera empresa (Tenant).</p>
                <div class="flex flex-col gap-3">
                    <InputText v-model="newTenantName" placeholder="Nombre de la empresa" />
                    <Button label="Crear Empresa" @click="createTenant" :disabled="!newTenantName" />
                </div>
            </template>
        </Card>
    </div>

    <div v-else class="grid grid-cols-1 md:grid-cols-3 gap-6">
      <!-- Upload Stats -->
      <Card class="col-span-1 border-l-4 border-primary">
        <template #content>
          <div class="flex items-center gap-4">
            <div class="bg-primary/10 p-3 rounded-xl text-primary">
              <FileText :size="32" />
            </div>
            <div>
              <p class="text-surface-400 text-sm">Documentos Totales</p>
              <h2 class="text-3xl font-bold">{{ documents.length }}</h2>
            </div>
          </div>
        </template>
      </Card>

      <!-- Upload Zone -->
      <Card class="col-span-2 relative overflow-hidden group">
        <template #content>
          <div class="flex items-center justify-between">
            <div class="space-y-1">
              <h3 class="text-xl font-bold">Cargar Nuevo Documento</h3>
              <p class="text-surface-400 text-sm">Soportado: PDF, TXT, CSV (Máx 10MB)</p>
            </div>
            <label class="cursor-pointer bg-primary text-primary-contrast px-6 py-3 rounded-lg font-bold hover:brightness-110 transition-all flex items-center gap-2">
              <Upload :size="20" v-if="!uploading" />
              <Loader2 :size="20" class="animate-spin" v-else />
              {{ uploading ? 'Procesando...' : 'Subir Archivo' }}
              <input type="file" class="hidden" @change="onFileUpload" accept=".pdf,.txt,.csv" :disabled="uploading" />
            </label>
          </div>
          <div class="absolute -right-4 -bottom-4 opacity-5 group-hover:opacity-10 transition-opacity rotate-12">
              <Upload :size="120" />
          </div>
        </template>
      </Card>
    </div>

    <!-- Documents Table -->
    <Card v-if="tenantStore.tenants.length > 0">
      <template #title>Biblioteca de Documentos</template>
      <template #content>
        <DataTable :value="documents" :loading="loading" class="p-datatable-sm">
          <template #empty>No hay documentos cargados para esta empresa.</template>
          <Column field="fileName" header="Nombre del Archivo" sortable></Column>
          <Column field="status" header="Estado">
              <template #body="slotProps">
                  <span :class="{
                      'text-green-400': slotProps.data.status === 'Completed',
                      'text-yellow-400': slotProps.data.status === 'Processing',
                      'text-red-400': slotProps.data.status === 'Failed'
                  }" class="flex items-center gap-2">
                      <CheckCircle v-if="slotProps.data.status === 'Completed'" :size="16" />
                      <Loader2 v-if="slotProps.data.status === 'Processing'" :size="16" class="animate-spin" />
                      {{ slotProps.data.status }}
                  </span>
              </template>
          </Column>
          <Column field="uploadedAt" header="Fecha de Carga" sortable>
              <template #body="slotProps">
                  {{ new Date(slotProps.data.uploadedAt).toLocaleString() }}
              </template>
          </Column>
          <Column header="Acciones">
              <template #body="slotProps">
                  <Button icon="pi pi-trash" severity="danger" text rounded />
              </template>
          </Column>
        </DataTable>
      </template>
    </Card>

    <!-- New Tenant Dialog -->
    <div v-if="showTenantDialog" class="fixed inset-0 bg-black/50 backdrop-blur-sm z-50 flex items-center justify-center p-4">
        <Card class="max-w-md w-full shadow-2xl">
            <template #title>Registrar Nueva Empresa</template>
            <template #content>
                <div class="flex flex-col gap-4 mt-2">
                    <div class="flex flex-col gap-2">
                        <label for="name" class="text-sm text-surface-400">Nombre de la Empresa</label>
                        <InputText id="name" v-model="newTenantName" autofocus placeholder="Ej. Corporación Senda" />
                    </div>
                    <div class="flex justify-end gap-2 mt-4">
                        <Button label="Cancelar" severity="secondary" text @click="showTenantDialog = false" />
                        <Button label="Guardar Empresa" icon="pi pi-check" @click="createTenant" :disabled="!newTenantName" />
                    </div>
                </div>
            </template>
        </Card>
    </div>
  </div>
</template>

<style scoped>
.p-card {
    background: var(--p-surface-950);
    border: 1px solid var(--p-surface-800);
}
</style>
