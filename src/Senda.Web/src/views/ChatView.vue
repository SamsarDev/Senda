<script setup>
import { ref, onMounted, nextTick } from 'vue';
import { useTenantStore } from '../stores/tenant';
import apiClient from '../api/client';
import { Send, User, Bot, Sparkles, Loader2, MessageSquare, Trash2 } from 'lucide-vue-next';
import Button from 'primevue/button';
import InputText from 'primevue/inputtext';
import Card from 'primevue/card';
import ScrollPanel from 'primevue/scrollpanel';
import Message from 'primevue/message';

const tenantStore = useTenantStore();
const messages = ref([]);
const input = ref('');
const loading = ref(false);
const sessionId = ref(null);
const scrollContainer = ref(null);

const scrollToBottom = async () => {
    await nextTick();
    if (scrollContainer.value) {
        const el = scrollContainer.value.$el.querySelector('.p-scrollpanel-content');
        if (el) el.scrollTop = el.scrollHeight;
    }
};

const sendMessage = async () => {
    if (!input.value.trim() || !tenantStore.currentTenantId || loading.value) return;

    const userMessage = input.value;
    messages.value.push({ role: 'User', content: userMessage });
    input.value = '';
    loading.value = true;
    
    await scrollToBottom();

    try {
        const response = await apiClient.post('/Chat/send', {
            sessionId: sessionId.value,
            customerIdentifier: 'admin-user', // Mock identifier for testing
            message: userMessage
        });

        sessionId.value = response.data.sessionId;
        messages.value.push({ 
            role: 'Assistant', 
            content: response.data.reply,
            context: response.data.sourceContext
        });
    } catch (err) {
        console.error('Chat error', err);
        messages.value.push({ 
            role: 'System', 
            content: 'Lo siento, hubo un error al procesar tu mensaje. Verifica que los servicios de IA estén activos.',
            isError: true
        });
    } finally {
        loading.value = false;
        await scrollToBottom();
    }
};

const clearChat = () => {
    messages.value = [];
    sessionId.value = null;
};
</script>

<template>
  <div class="h-full flex flex-col p-4 md:p-6 bg-surface-900 overflow-hidden">
    <div class="max-w-4xl w-full mx-auto flex-1 flex flex-col gap-4 overflow-hidden">
      
      <!-- Chat Header -->
      <div class="flex justify-between items-center bg-surface-950 p-4 rounded-xl border border-surface-800">
          <div class="flex items-center gap-3">
              <div class="bg-primary/20 p-2 rounded-lg text-primary">
                  <MessageSquare :size="20" />
              </div>
              <div>
                  <h2 class="font-bold text-white">Prueba del Concierge</h2>
                  <p class="text-xs text-surface-400">Interactúa con el conocimiento indexado.</p>
              </div>
          </div>
          <Button icon="pi pi-refresh" label="Limpiar Sesión" severity="secondary" text class="text-sm" @click="clearChat" />
      </div>

      <!-- Messages Area -->
      <Card class="flex-1 overflow-hidden ChatCard">
        <template #content>
            <ScrollPanel ref="scrollContainer" style="height: 100%" class="custom-scroll">
                <div class="flex flex-col gap-6 p-2">
                    <div v-if="messages.length === 0" class="flex flex-col items-center justify-center py-20 text-surface-500 space-y-4">
                        <div class="bg-surface-800 p-6 rounded-full">
                            <Sparkles :size="48" class="text-primary/40" />
                        </div>
                        <p>Haz una pregunta para ver la IA en acción.</p>
                    </div>

                    <div v-for="(msg, index) in messages" :key="index" 
                         :class="['flex w-full', msg.role === 'User' ? 'justify-end' : 'justify-start']">
                        
                        <div :class="[
                            'max-w-[85%] p-4 rounded-2xl space-y-2 shadow-lg',
                            msg.role === 'User' ? 'bg-primary text-primary-contrast rounded-tr-none' : 
                            msg.isError ? 'bg-red-900/30 border border-red-500 text-red-200' : 'bg-surface-800 text-surface-0 rounded-tl-none'
                        ]">
                            <div class="flex items-center gap-2 text-xs opacity-70 mb-1">
                                <User v-if="msg.role === 'User'" :size="14" />
                                <Bot v-else :size="14" />
                                <span class="uppercase font-bold tracking-wider">{{ msg.role }}</span>
                            </div>
                            <p class="leading-relaxed whitespace-pre-wrap">{{ msg.content }}</p>
                            
                            <!-- Source Context if available -->
                            <div v-if="msg.context" class="mt-3 pt-3 border-t border-surface-700">
                                <p class="text-[10px] font-bold uppercase tracking-widest text-primary mb-1">Contexto Recuperado (RAG):</p>
                                <p class="text-[11px] italic text-surface-400 line-clamp-2">{{ msg.context }}</p>
                            </div>
                        </div>
                    </div>

                    <div v-if="loading" class="flex justify-start">
                        <div class="bg-surface-800 p-4 rounded-2xl rounded-tl-none flex items-center gap-3">
                            <Loader2 :size="16" class="animate-spin text-primary" />
                            <span class="text-sm text-surface-400">La IA está pensando...</span>
                        </div>
                    </div>
                </div>
            </ScrollPanel>
        </template>
      </Card>

      <!-- Input Area -->
      <div class="flex gap-2 bg-surface-950 p-2 rounded-2xl border border-surface-800">
          <InputText 
            v-model="input" 
            placeholder="Escribe tu pregunta aquí..." 
            class="flex-1 border-none bg-transparent focus:ring-0 text-white" 
            @keyup.enter="sendMessage"
            :disabled="loading || !tenantStore.currentTenantId"
          />
          <Button 
            icon="pi pi-send" 
            @click="sendMessage" 
            :disabled="!input.trim() || loading || !tenantStore.currentTenantId"
            class="rounded-xl px-6"
          />
      </div>
      <p v-if="!tenantStore.currentTenantId" class="text-center text-xs text-red-400">
          * Debes seleccionar una empresa en el Dashboard para chatear.
      </p>
    </div>
  </div>
</template>

<style scoped>
.ChatCard {
    background: var(--p-surface-950);
    border: 1px solid var(--p-surface-800);
    height: 100%;
}

:deep(.p-card-body) {
    height: 100%;
    padding: 0;
}

:deep(.p-card-content) {
    height: 100%;
    padding: 0;
}

.custom-scroll :deep(.p-scrollpanel-bar) {
    background: var(--p-surface-700);
}
</style>
