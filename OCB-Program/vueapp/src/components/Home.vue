<template>
  <div class="wrapper">
    <div class="block">
      <div class="columns is-mobile is-centered">
        <div class="column is-half">
          <div class="card">
            <div class="columns is-mobile is-centered">
              <button
                class="button is-dark"
                @click="showUploader"
              >Select OCB to add to Database</button>
            </div>

          </div>
          <div
            class="card"
            v-if="showUploaderComponent"
          >
            <FileUploader />
            <footer class="card-footer">
              <a
                href="#"
                class="card-footer-item"
                @click="hide"
              >Close</a>
            </footer>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import FileUploader from "./FileUploader.vue"; // Обновите путь, если он другой

export default {
  components: {
    FileUploader,
  },
  data() {
    return {
      showUploaderComponent: false,
    };
  },
  methods: {
    showUploader() {
      this.showUploaderComponent = true;
    },
    hide() {
      this.showUploaderComponent = false;
    },
  },
};
</script>

<style>
.wrapper {
  padding: 3rem;
}
.card {
  padding: 3rem;
}
</style>