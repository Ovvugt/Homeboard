<script setup lang="ts">
import { onMounted } from 'vue'
import { RouterLink } from 'vue-router'
import { useBoardsStore } from '@/stores/boards'

const boards = useBoardsStore()
onMounted(() => boards.loadList())
</script>

<template>
  <aside class="w-56 shrink-0 bg-white dark:bg-gray-950 border-r border-gray-200 dark:border-gray-800 px-3 py-4 flex flex-col gap-1">
    <div class="text-xs uppercase tracking-wider text-gray-500 px-2 mb-2">Boards</div>
    <RouterLink
      v-for="b in boards.list"
      :key="b.id"
      :to="`/b/${b.slug}`"
      class="px-3 py-1.5 rounded-md text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800"
      active-class="bg-primary-50 dark:bg-primary-950/40 text-primary-800 dark:text-primary-300 font-medium"
    >
      {{ b.name }}
    </RouterLink>
    <div class="mt-auto">
      <RouterLink
        to="/settings"
        class="px-3 py-1.5 rounded-md text-sm text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 block"
      >
        Settings
      </RouterLink>
    </div>
  </aside>
</template>
