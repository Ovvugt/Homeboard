<script setup lang="ts">
import { RouterView } from 'vue-router'
import AppHeader from '@/components/layout/AppHeader.vue'
import AppSidebar from '@/components/layout/AppSidebar.vue'
import { useThemeStore } from '@/stores/theme'

useThemeStore()
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
