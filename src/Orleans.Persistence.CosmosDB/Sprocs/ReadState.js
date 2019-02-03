function ReadState() {
  var context = getContext();
  var collection = context.getCollection();
  var response = context.getResponse();
  
  // Select all entities in the Partition
  var query = 'SELECT * FROM c';
  var accept = collection.queryDocuments(collection.getSelfLink(), query, {},
    function (err, docs) {
      if (err) throw new Error("Error: " + err.message);

      if (docs.length === 0) {
        response.setBody(null);
      } else {
        response.setBody(docs);
      }
    });
}
