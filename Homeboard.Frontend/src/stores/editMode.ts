import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useEditModeStore = defineStore('editMode', () => {
  const editing = ref(false)
  function toggle() { editing.value = !editing.value }
  return { editing, toggle }
})
