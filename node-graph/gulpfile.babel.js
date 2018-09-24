"use strict";

import path from "path";
import gulp from "gulp";
import tsc from "gulp-typescript";
import { exec } from "child_process";
import tslint from "gulp-tslint";
import clean from "gulp-clean";
import rename from "gulp-rename";
import mocha from "gulp-mocha";
import sourcemaps from "gulp-sourcemaps";
import nodemon from "gulp-nodemon";
import webpack from "webpack";
import chalk from "chalk";

// Development unless told otherwise.
process.env.NODE_ENV = "development";

// Running node instance.
var node;

// Global browser sync instance.
var browserSync = require('browser-sync').create();

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
gulp.task("clean", function doCleanWork() {
  return gulp.src([
    paths.tscripts.destDir,
    paths.tscripts.packageDir
  ], { read: false, allowEmpty: true }).pipe(clean());
});

// ** Linting ** //
gulp.task("lint", function doLintWork() {
  return gulp.src(paths.tscripts.srcFiles)
    .pipe(tslint({ formatter: "verbose" }))
    .pipe(tslint.report());
});

// ** Compilation ** //
gulp.task("preprocess", function doPreProcessWork(done) {
  return gulp.src("src/app/config-sample.ts").
    pipe(rename("config.ts")).
    pipe(gulp.dest("src/app", { overwrite: false }))
});
gulp.task("compile:typescript", function doCompileWork() {
  var project = tsc.createProject("tsconfig.json", { declaration: true });
  var built = gulp.src(paths.tscripts.srcFiles)
    .pipe(sourcemaps.init())
    .pipe(project());
  return built.js
    // Write inline source maps.
    .pipe(sourcemaps.write())
    .pipe(gulp.dest(paths.tscripts.destDir));
});
gulp.task("build", gulp.series("clean", "lint", "preprocess", "compile:typescript"));

// ** Serve **
gulp.task("serveSrc", function doServeSrcWork(done) {
  // Launch express (using nodemon to monitor).
  var called = false;
  // Use nodemon to run express app.
  // Restart our server whenever code changes.
  return nodemon({
    script: path.resolve(paths.tscripts.serveDir, "devServe.js"),
    ignore: ["node_modules/"],
    // Source files to watch that'll cause reload.
    watch: ["src"],
    // Environment variables.
    env: { "DEBUG": "express:router" },
    // Extensions to watch.
    ext: "ts html css",
    // Tasks to run on file changes.
    tasks: function (changedFiles) {
      if (changedFiles !== undefined) {
        changedFiles.forEach(element => {
          console.log(`File ${element} changed`);
        });
      }
      if (browserSync.active) {
        browserSync.notify("Recompiling, please wait", 5000);
      }
      return ["compile:typescript"];
    }
  }).on("start", function () {
    // Avoid nodemon being started multiple times.
    if (!called) {
      called = true;
      // Wait for the main load to complete.
      setTimeout(function () {
        done();
      }, 2000);
    }
  }).on("restart", function () {
    setTimeout(function () {
      // When nodemon restarts the server, instruct browsersync to reload.
      browserSync.notify("Reloading");
      browserSync.reload({ stream: false });
      done();
    }, 2000);
  });
});

// ** Watching **
gulp.task("watch", function doWatchWork() {
  // If src files change, recompile them.
  return gulp.watch(paths.tscripts.srcFiles, gulp.series("compile:typescript"));
});

// ** Browser Sync **
gulp.task("browser-sync", gulp.series("serveSrc", function doBrowserSyncWork(done) {
  // Initialize browser sync.
  browserSync.init({
    proxy: "localhost:3000",  // local node app address
    port: 5000,               // use *different* port than above
    notify: true,             // We want to know about changes.
  });
  done();
}));

// ** Packaging **
gulp.task("package", gulp.series("build", function doPackageWork(done) {
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
gulp.task("serve:dist", gulp.series("package", function doProdServeWork(done) {
  exec("node " + path.resolve(paths.tscripts.serveDir, "prodServe.js"));
  done();
}));

// ** Unit Tests ** //
gulp.task("run-tests", function doTestsWork() {
  return gulp.src(paths.tscripts.destDir + "/*.test.js", { read: false }).
    pipe(mocha({
      reporter: 'spec'
    }));
});
gulp.task("test", gulp.series("build", "run-tests"));

// ** Default ** //
gulp.task("serve", gulp.series("build", "browser-sync"));
gulp.task("default", gulp.series("serve"));
