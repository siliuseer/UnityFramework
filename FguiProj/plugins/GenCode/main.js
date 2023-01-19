"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
exports.onDestroy = exports.onPublish = void 0;
const csharp_1 = require("csharp");
const GenCode_CSharp_1 = require("./GenCode_CSharp");
function onPublish(handler) {
    handler.genCode = false; //prevent default output
    GenCode_CSharp_1.GenCode_CSharp(handler);
}
exports.onPublish = onPublish;
function onDestroy() {
    //do cleanup here
}
exports.onDestroy = onDestroy;
