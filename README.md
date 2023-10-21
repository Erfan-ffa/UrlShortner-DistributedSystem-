# Url_Shortner_Project

- How to run the project?
  1- First of all run the docker compose file.

  2- After that run the follwoing command in your terminal:
  docker exec -it mongo-replica-container mongosh --eval "rs.initiate({_id:'rs0', members: [{_id:0, host: 'mongo-replica-container'}]})"

  3- You could ensure the replicaset is initated properly by running this command:
    docker exec -it mongo-replica-container mongosh --eval "rs.status()"

  4- Done :)
