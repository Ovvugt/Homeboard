<script setup lang="ts">
import { RouterLink } from 'vue-router'
import { SunIcon, MoonIcon, ComputerDesktopIcon, PencilSquareIcon, EyeIcon } from '@heroicons/vue/24/outline'
import { useThemeStore } from '@/stores/theme'
import { useEditModeStore } from '@/stores/editMode'

const theme = useThemeStore()
const edit = useEditModeStore()
</script>

<template>
  <header class="flex items-center justify-between h-14 px-4 bg-white dark:bg-gray-950 border-b border-gray-200 dark:border-gray-800">
    <RouterLink to="/" class="font-display text-2xl text-primary-700 dark:text-primary-400">Homeboard</RouterLink>
    <div class="flex items-center gap-2">
      <button
        type="button"
        class="inline-flex items-center gap-1.5 px-3 py-1.5 rounded-md text-sm transition-colors"
        :class="edit.editing
          ? 'bg-primary-600 text-white hover:bg-primary-700'
          : 'bg-gray-100 dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-gray-200 dark:hover:bg-gray-700'"
        @click="edit.toggle()"
      >
        <component :is="edit.editing ? EyeIcon : PencilSquareIcon" class="w-4 h-4" />
        {{ edit.editing ? 'Done' : 'Edit' }}
      </button>
      <button
        type="button"
        class="p-2 rounded-md text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800"
        :title="`Theme: ${theme.preference}`"
        @click="theme.toggle()"
      >
        <SunIcon v-if="theme.preference === 'light'" class="w-5 h-5" />
        <MoonIcon v-else-if="theme.preference === 'dark'" class="w-5 h-5" />
        <ComputerDesktopIcon v-else class="w-5 h-5" />
      </button>
    </div>
  </header>
</template>
