#!/bin/bash

# Helper script for Vue 3 + PrimeVue + Tailwind development in Senda project

# Function: Initialize the project if not exists
init_project() {
    local target_dir="src/Senda.Web"
    if [ ! -d "$target_dir" ]; then
        echo "Initializing Vue 3 project in $target_dir..."
        # Using npx create-vite with default answers for non-interactive mode
        npx -y create-vite@latest "$target_dir" --template vue
    else
        echo "Project directory $target_dir already exists."
    fi
}

# Function: Install core dependencies
install_deps() {
    local target_dir="src/Senda.Web"
    echo "Installing UI dependencies in $target_dir..."
    cd "$target_dir" || exit
    
    # UI and Styling
    npm install primevue @primevue/themes tailwindcss postcss autoprefixer lucide-vue-next
    
    # State and Routing
    npm install pinia vue-router axios
    
    echo "Dependencies installed."
}

# Check for arguments
case "$1" in
    "init")
        init_project
        ;;
    "deps")
        install_deps
        ;;
    *)
        echo "Usage: $0 {init|deps}"
        ;;
esac
