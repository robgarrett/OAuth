import * as express from "express";
import * as path from "path";
import * as webpack from "webpack";
import * as webpackMiddleware from "webpack-dev-middleware";
//

const paths = {
  webpackConfig: path.resolve(__dirname, "../../webpack.config.dev"),
};

const port = 3000;
const app = express();
const compiler = webpack(require(paths.webpackConfig));

// Use web pack compiler to bundle and serve content.
app.use(webpackMiddleware(compiler, {
  publicPath: require(paths.webpackConfig).output.publicPath,
}));

// Start express.
app.listen(port, function (err) {
  console.log(`Express listening on port ${port}`);
  if (err) { console.log(err); }
});
