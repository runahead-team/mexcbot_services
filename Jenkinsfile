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
              changeset "src/multexbot.Api/**"
            }
          }
          steps {
            script {
              sh "docker build -t multexbot_api -f src/multexbot.Api/Dockerfile src"
              sh "docker tag multexbot_api:latest registry2.spdev.co/multexbot_api:latest"
              sh "docker push registry2.spdev.co/multexbot_api:latest"
            }
          }
        }
      }
    }

    stage('Deploy') {
      parallel {
        stage('multexbot') {
          steps {
            script {
              sh "docker stack deploy -c /root/spexchange/app/spexchange.yml spexchange --with-registry-auth"
            }
          }
        }
      }
    }
  }
}