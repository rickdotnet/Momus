job("Build/Push Momus") {
    // manually triggered
    startOn {} 
    host("Build and push to Docker Hub") {
	
        env["D_USER"] = "{{ project:docker-user }}"
		env["D_TOKEN"] = "{{ project:docker-token }}"
		env["D_VERSION"] = "{{ project:momus-docker-version }}"
		
		shellScript("Login to Docker Hub"){
            content = """
                docker login -u ${'$'}D_USER -p ${'$'}D_TOKEN
            """
        }
        
        dockerBuildPush  {
            file = "./Dockerfile"
            labels["author"] = "rickdotnet"
            tags {
                +"rickdotnet/momus:latest"
                +"rickdotnet/momus:${"$"}D_VERSION.${"$"}JB_SPACE_EXECUTION_NUMBER"
            }
        }
    }
}