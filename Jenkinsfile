pipeline {
  agent any
  stages {
    stage('Build') {
      parallel {
        stage('api') {
          when {
            anyOf {
              changeset "src/sp.Core.Mailer/**"
              changeset "src/sp.Core.Token/**"
              changeset "src/sp.Core/**"
              changeset "src/mexcbot.Api/**"
            }
          }
          steps {
            script {
              sh "docker build -t mexcbot_api -f src/mexcbot.Api/Dockerfile src"
              sh "docker tag mexcbot_api:latest registry2.spdev.co/mexcbot_api:latest"
              sh "docker push registry2.spdev.co/mexcbot_api:latest"
            }
          }
        }
      }
    }

    stage('Deploy') {
      parallel {
        stage('mexcbot') {
          steps {
            script {
              sh "docker stack deploy -c /root/multex_bot/app/mexcbot.yml mexcbot --with-registry-auth"
            }
          }
        }
      }
    }
  }
}