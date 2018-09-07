import * as express from "express";
import * as path from "path";
import * as webpack from "webpack";
import * as webpackMiddleware from "webpack-dev-middleware";
//

const paths = {
  webpackConfig: path.resolve(__dirname, "../../webpack.config.dev"),
  entryPoint: path.resolve(__dirname, "../src/index.html"),
};

const port = 3000;
const app = express();
const compiler = webpack(require(paths.webpackConfig));

// Use web pack compiler to bundle.
app.use(webpackMiddleware(compiler, {
   publicPath: require(paths.webpackConfig).output.publicPath,
}));

app.get("/", function(req, res) {
  res.sendFile(paths.entryPoint);
});

app.listen(port, function(err) {
  if (err) { console.log(err); }
});
