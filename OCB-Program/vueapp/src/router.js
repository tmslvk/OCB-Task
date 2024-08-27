import { createRouter, createWebHistory } from 'vue-router'
import Home from './components/Home.vue'

const router = createRouter({
    linkActiveClass: 'is-active',
    history: createWebHistory(),
    routes: [
        {
            path: '/Home',
            name: 'Home',
            component: Home,
        },
    ]
})

export default router