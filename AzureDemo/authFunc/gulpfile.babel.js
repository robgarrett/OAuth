import gulp from "gulp";
import { spawn } from "child_process";

let { clean, build, restore, publish } = require("gulp-dotnet-cli");

// ** Clean ** /
gulp.task("clean", () => {
    return gulp.src("**/*.csproj", { read: false }).pipe(clean());
});

// ** Build ** /
gulp.task("prebuild", (done) => {
    return done();
});

gulp.task("postbuild", (done) => {
    return done();
});

gulp.task("restore", () => {
    return gulp.src("**/*.csproj", { read: false }).pipe(restore());
});

gulp.task("compile", () => {
    return gulp.src("**/*.csproj", { read: false }).pipe(build());
});

gulp.task("dockerize", (done) => {
    var cmd = spawn("docker-compose", ["build", "auth-func-dev"], { stdio: 'inherit' });
    cmd.on("close", (code) => {
        done(code);
    });
});

gulp.task("build", gulp.series("prebuild", "restore", "compile", "postbuild"));

gulp.task("build:full", gulp.series("prebuild", "restore", "compile", "postbuild", "dockerize"));

// ** Publish ** /
gulp.task("publish:build", () => {
    return gulp.src("**/*.csproj", { read: false }).pipe(publish({ configuration: "Release" }));
});
gulp.task("postpublish", (done) => {
    return done();
});
gulp.task("publish", gulp.series("publish:build", "postpublish"));

// ** Run ** /
gulp.task("run-build", (done) => {
    var cmd = spawn("docker-compose", ["run", "--service-ports", "auth-func-dev"], { stdio: 'inherit' });
    cmd.on("close", (code) => {
        done(code);
    });
});

gulp.task("run", gulp.series("build:full", "run-build"));

// ** Default ** /
gulp.task("default", gulp.series("run"));
