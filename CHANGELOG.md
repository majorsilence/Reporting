# Changelog

Versions are `year.release.build`. `release/26.0.x` is the stable 26.0.x line and `main` is the 27.x.x line.

## Unreleased (27.0.0)

### Breaking changes

* **Web designer schema endpoint (`/schema`) no longer accepts a client-supplied connection string.**
  The designer's field discovery now sends the data source name, and the server looks up the provider and
  connection string you configured in `RdlDesignerOptions.SchemaConnections` (name to provider and connection string).
  This fixes the CodeQL `cs/resource-injection` alerts: before, any client that could reach the endpoint chose which
  file, URL or database the server connected to.

  To upgrade, add an entry named after the RDL data source:

  ```csharp
  builder.Services.AddRdlDesigner(o =>
  {
      o.SchemaConnections["Sales"] = new RdlSchemaConnection
      {
          DataProvider = "Json",
          ConnectionString = "file=/srv/data/sales.json",
      };
  });
  ```

  Setting `RdlDesignerOptions.AllowClientConnectionStrings = true` restores the old behaviour for database providers
  only, and should be used only when the endpoint is reachable by trusted users. The `filedirectory`, `xml`,
  `webservice`, `weblog`, `text`, `json` and `itunes` providers are always refused from the client and only work through
  `SchemaConnections`.

### Security

* `RdlAsp.Mvc` report lookup can no longer escape the reports folder with `..`, rooted paths or wildcards
  (CodeQL `cs/path-injection`). Also fixed in 26.0.x.
