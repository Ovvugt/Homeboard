<script setup lang="ts">
import { onMounted, onBeforeUnmount } from 'vue'
import { RouterView } from 'vue-router'
import AppHeader from '@/components/layout/AppHeader.vue'
import AppSidebar from '@/components/layout/AppSidebar.vue'
import { useThemeStore } from '@/stores/theme'
import { useEditModeStore } from '@/stores/editMode'

useThemeStore()
const edit = useEditModeStore()

function isTypingTarget(t: EventTarget | null): boolean {
  if (!(t instanceof HTMLElement)) return false
  if (t.isContentEditable) return true
  const tag = t.tagName
  return tag === 'INPUT' || tag === 'TEXTAREA' || tag === 'SELECT'
}

function isModalOpen(): boolean {
  // Modal.vue teleports a fixed inset-0 z-50 backdrop to <body> while open
  return !!document.querySelector('.fixed.inset-0.z-50')
}

function onKey(e: KeyboardEvent) {
  if (e.ctrlKey || e.metaKey || e.altKey) return
  if (isTypingTarget(e.target)) return
  if (isModalOpen()) return // let modal handle its own Escape, and don't enter edit mode behind it

  if (e.key === 'e' || e.key === 'E') {
    if (!edit.editing) {
      edit.toggle()
      e.preventDefault()
    }
  } else if (e.key === 'Escape') {
    if (edit.editing) {
      edit.toggle()
      e.preventDefault()
    }
  }
}

onMounted(() => document.addEventListener('keydown', onKey))
onBeforeUnmount(() => document.removeEventListener('keydown', onKey))
</script>

<template>
  <div class="h-dvh flex flex-col bg-gray-50 dark:bg-gray-900 transition-colors text-gray-900 dark:text-gray-100">
    <AppHeader />
    <div class="flex flex-1 overflow-hidden">
      <AppSidebar />
      <main class="flex-1 overflow-y-auto">
        <RouterView />
      </main>
    </div>
  </div>
</template>
