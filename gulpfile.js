/// <binding Clean='clean' />
"use strict";

var gulp = require("gulp"),
    rimraf = require("rimraf"),
    concat = require("gulp-concat"),
    cssmin = require("gulp-cssmin"),
    uglify = require("gulp-uglify"),
    rename = require('gulp-rename');

var webroot = "./wwwroot/";

var paths = {
    js: webroot + "js/**/*.js",
    minJs: webroot + "js/**/*.min.js",
    pathJs: webroot + "js/",
    css: webroot + "css/**/*.css",
    minCss: webroot + "css/**/*.min.css",
    pathCss: webroot + "css/"
};

gulp.task("clean:js", function (cb) {
    rimraf(paths.minJs, cb);
});

gulp.task("clean:css", function (cb) {
    rimraf(paths.minCss, cb);
});

gulp.task("clean", ["clean:js", "clean:css"]);

gulp.task("min:js", function () {
    return gulp.src([paths.js, "!" + paths.minJs], { base: "." })
        .pipe(rename({
            suffix: '.min'
        }))
        .pipe(uglify())
        .pipe(gulp.dest(paths.pathJs));
});

gulp.task("min:css", function () {
    return gulp.src([paths.css, "!" + paths.minCss])
        .pipe(rename({
            suffix: '.min'
        }))
        .pipe(cssmin())
        .pipe(gulp.dest(paths.pathCss));
});

gulp.task("min", ["min:js", "min:css"]);
