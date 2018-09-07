"use strict";

import path from "path";
import gulp from "gulp";
import tsc from "gulp-typescript";
import { exec } from "child_process";
import tslint from "gulp-tslint";
import clean from "gulp-clean";
import mocha from "gulp-mocha";
import sourcemaps from "gulp-sourcemaps";
import browserSync from "browser-sync";
import nodemon from "nodemon";
import webpack from "webpack";
import chalk from "chalk";

// Development unless told otherwise.
process.env.NODE_ENV = "development";

// Running node instance.
var node;

var paths = {
  tscripts: {
    // All source files, including unit tests.
    srcFiles: [
      "src/**/*.ts",
    ],
    destDir: "lib",
    packageDir: "dist",
    serveDir: "lib/serve",
  }
};

// ** Clean ** /
gulp.task("clean", function doWork() {
  return gulp.src([
    paths.tscripts.destDir,
    paths.tscripts.packageDir
  ], { read: false, allowEmpty: true }).pipe(clean());
});

// ** Linting ** //
gulp.task("lint", function doWork() {
  return gulp.src(paths.tscripts.srcFiles)
    .pipe(tslint({ formatter: "verbose" }))
    .pipe(tslint.report());
});

// ** Compilation ** //
gulp.task("compile:typescript", function doWork() {
  var project = tsc.createProject("tsconfig.json", { declaration: true });
  var built = gulp.src(paths.tscripts.srcFiles)
    .pipe(sourcemaps.init())
    .pipe(project());
  return built.js
    // Write inline source maps.
    .pipe(sourcemaps.write())
    .pipe(gulp.dest(paths.tscripts.destDir));
});
gulp.task("build", gulp.series("clean", "lint", "compile:typescript"));

// ** Serve **
gulp.task("serveSrc", function doWork(done) {
  // Launch express (using nodemon to monitor).
  var called = false;
  // Use nodemon to run express app.
  // Restart our server whenever code changes.
  return nodemon({
    script: path.resolve(paths.tscripts.serveDir, "devServe.js"),
    ignore: ["node_modules/"]
  }).on("start", function () {
    if (!called) {
      called = true;
      // Wait for the main load to complete.
      setTimeout(function () {
        done();
      }, 2000);
    }
  }).on("restart", function () {
    // When nodemon restarts the server, instruct browsersync to reload.
    setTimeout(function () {
      browserSync.reload({ stream: false })
    }, 2000);
  });
});

// ** Watching **
gulp.task("watch", function doWork() {
  // If src files change, recompile them.
  // This will cause new app/lib/*.js files, and nodemon will pick these up and
  // restart express.
  return gulp.watch(paths.tscripts.srcFiles, gulp.series("compile:typescript"));
});

// ** Browser Sync **
gulp.task("browser-sync", gulp.series("serveSrc", function doWork() {
  // Initialize browser sync.
  browserSync({
    proxy: "localhost:3000",  // local node app address
    port: 5000,  // use *different* port than above
    notify: true
  });
}));

// ** Packaging **
gulp.task("package", gulp.series("build", function doWork(done) {
  // Call web pack to package distribution build.
  process.env.NODE_ENV = "production";
  const config = require("./webpack.config.prod");
  return new Promise(resolve => {
    // Call web pack.
    webpack(config, (err, stats) => {
      if (err) {
        // Fatal Error, stop here.
        console.log(chalk.red('Webpack', err));
        return 1;
      }
      const jsonStats = stats.toJson();
      if (jsonStats.hasErrors) {
        return jsonStats.errors.map(error => console.log(chalk.red(error)));
      }
      if (jsonStats.hasWarnings) {
        console.log(chalk.yellow("Webpack generated the following errors:"));
        return jsonStats.warnings.map(warning => console.log(chalk.yellow(warning)));
      }
      console.log(`Webpack stats: ${stats}`);
      console.log(chalk.green("App packaged in dist folder"));
      return 0;
    });
    // Signal completion.
    done();
  });
}));

// ** Production Serve **
gulp.task("serve:dist", gulp.series("package", function doWork(done) {
  exec("node " + path.resolve(paths.tscripts.serveDir, "prodServe.js"));
  done();
}));

// ** Unit Tests ** //
gulp.task("run-tests", function doWork(done) {
  return gulp.src(paths.tscripts.destDir + "/*.test.js", { read: false }).
    pipe(mocha({
      reporter: 'spec'
    }));
});
gulp.task("test", gulp.series("build", "run-tests"));

// ** Default ** //
gulp.task("serve", gulp.series("build", "browser-sync", "watch"));
gulp.task("default", gulp.series("serve"));