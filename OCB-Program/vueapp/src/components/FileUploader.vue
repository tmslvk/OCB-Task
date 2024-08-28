<template>
  <div class="block">
    <div class="columns is-three-quarters">
      <title>Upload files</title>
      <input
        class="is-medium"
        type="file"
        @change="handleFileUpload"
        accept=".xlsx,.xls"
      />
      <button
        class="button is-success"
        @click="uploadFile"
      >Upload</button>
      <p v-if="uploadStatus">{{ uploadStatus }}</p>
    </div>
    <div class="block">
      <div class="block">
        <div class="columns is-centered">
          <h2>Uploaded Files</h2>
        </div>

      </div>

      <ul>
        <li
          v-for="file in files"
          :key="file.id"
        >
          <a @click.prevent="fetchDocumentData(file.id)">{{ file.filename }}</a>
        </li>
      </ul>

      <!-- Отображение данных документа -->
      <div v-if="document">
        <div class="box">
          <div class="content">
            <h2>Document Data</h2>
            <p><strong>Bank Name:</strong> {{ document.bankName }}</p>
            <p><strong>Start Date:</strong> {{ new Date(document.startDate).toLocaleDateString() }}</p>
            <p><strong>End Date:</strong> {{ new Date(document.endDate).toLocaleDateString() }}</p>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<script>
import axios from "axios";

export default {
  data() {
    return {
      selectedFile: null,
      files: [], // Список загруженных файлов
      document: null, // Данные документа для выбранного файла
      uploadStatus: "", // Статус загрузки файла
    };
  },
  methods: {
    handleFileUpload(event) {
      this.selectedFile = event.target.files[0];
      console.log("Selected file:", this.selectedFile);
      this.uploadStatus = this.selectedFile
        ? `Selected file: ${this.selectedFile.name}`
        : "No file selected.";
    },
    async uploadFile() {
      if (!this.selectedFile) {
        this.uploadStatus = "Please select a file first.";
        return;
      }

      const formData = new FormData();
      formData.append("file", this.selectedFile);
      for (let [key, value] of formData.entries()) {
        console.log("FormData entry:", key, value);
      }
      try {
        const response = await axios.post(
          "https://localhost:7235/api/Home/upload-excel",
          formData,
          {
            headers: { "Content-Type": "multipart/form-data" },
          }
        );
        console.log(response.data);
        this.uploadStatus = `File uploaded successfully! Document ID: ${response.data.documentId}`;
        await this.fetchUploadedFiles();
      } catch (error) {
        console.error("Error uploading file:", error);
        this.uploadStatus = "Failed to upload file.";
      }
    },
    async fetchUploadedFiles() {
      try {
        const response = await axios.get(
          "https://localhost:7235/api/Home/files"
        );
        this.files = response.data;
        console.log(response.data);
      } catch (error) {
        console.error("Error fetching files:", error);
      }
    },
    async fetchDocumentData(id) {
      try {
        const response = await axios.get(
          `https://localhost:7235/api/Home/document/${id}`
        );
        this.document = response.data;
        console.log(response.data);
      } catch (error) {
        console.error("Error fetching document data:", error);
      }
    },
  },
  mounted() {
    this.fetchUploadedFiles();
  },
};
</script>

<style scoped>
.box {
  background-color: rgb(188, 134, 199);
  color: white;
}
.p {
  padding: 1rem;
}
.block {
  padding: 1rem;
  margin: 1px;
}
.ul {
  padding: 1rem;
  margin: 3px;
}
.card-footer {
  background-color: rgb(221, 72, 72);
}
</style>
