job("Build/Push to Docker registry") {
    docker {
        build {
            file = "./Dockerfile"
        }

        push("{{ project:docker-image }}") {
            tags("latest")
        }
    }
}