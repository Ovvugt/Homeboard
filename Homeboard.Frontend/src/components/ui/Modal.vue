<script setup lang="ts">
import { onMounted, onUnmounted } from 'vue'

const props = defineProps<{ open: boolean; title?: string }>()
const emit = defineEmits<{ close: [] }>()

function onKey(e: KeyboardEvent) {
  if (e.key === 'Escape' && props.open) emit('close')
}

onMounted(() => document.addEventListener('keydown', onKey))
onUnmounted(() => document.removeEventListener('keydown', onKey))
</script>

<template>
  <Teleport to="body">
    <Transition name="fade">
      <div
        v-if="open"
        class="fixed inset-0 z-50 flex items-start justify-center p-4 sm:p-8 bg-black/40 backdrop-blur-sm"
        @click.self="emit('close')"
      >
        <div class="w-full max-w-lg max-h-[calc(100dvh-4rem)] flex flex-col rounded-xl bg-white dark:bg-gray-900 border border-gray-200 dark:border-gray-700 shadow-xl overflow-hidden">
          <header v-if="title" class="px-5 py-3 border-b border-gray-200 dark:border-gray-800 shrink-0">
            <h2 class="font-display text-xl text-gray-900 dark:text-gray-100">{{ title }}</h2>
          </header>
          <div class="p-5 overflow-y-auto grow">
            <slot />
          </div>
          <footer class="px-5 py-3 border-t border-gray-200 dark:border-gray-800 flex justify-end gap-2 shrink-0">
            <slot name="footer" :close="() => emit('close')" />
          </footer>
        </div>
      </div>
    </Transition>
  </Teleport>
</template>

<style scoped>
.fade-enter-active, .fade-leave-active { transition: opacity 0.15s ease; }
.fade-enter-from, .fade-leave-to { opacity: 0; }
</style>
