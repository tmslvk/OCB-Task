<template>
  <div class="app">
  </div>
  <div class="block">
  </div>
  <router-view />
</template>

<script>
import { mapState } from "vuex";
import Home from "./components/Home.vue";

export default {
  components: {
    Home,
  },
};
</script>

<style scoped>
.app {
  background: rgb(6, 92, 117);
  background: linear-gradient(
    352deg,
    rgba(6, 92, 117, 1) 7%,
    rgba(39, 62, 189, 1) 42%,
    rgba(9, 19, 158, 1) 86%
  );
}
</style>


