<script setup lang="ts">
defineProps<{ label: string; hint?: string }>()
</script>

<template>
  <label class="block">
    <span class="text-sm font-medium text-gray-700 dark:text-gray-300">{{ label }}</span>
    <slot />
    <span v-if="hint" class="block text-xs text-gray-500 mt-1">{{ hint }}</span>
  </label>
</template>
