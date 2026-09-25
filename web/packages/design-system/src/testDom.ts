import { JSDOM } from "jsdom";
import * as React from "react";

const dom = new JSDOM("<!doctype html><html><body></body></html>", {
  url: "http://localhost/",
});

Object.defineProperties(globalThis, {
  // tsx preserves the Next.js JSX setting while running these package-level
  // tests, so expose React for components compiled with the classic runtime.
  React: { value: React, configurable: true },
  window: { value: dom.window, configurable: true },
  document: { value: dom.window.document, configurable: true },
  navigator: { value: dom.window.navigator, configurable: true },
  HTMLElement: { value: dom.window.HTMLElement, configurable: true },
  HTMLDialogElement: {
    value: dom.window.HTMLDialogElement,
    configurable: true,
  },
  Node: { value: dom.window.Node, configurable: true },
  MutationObserver: { value: dom.window.MutationObserver, configurable: true },
  getComputedStyle: { value: dom.window.getComputedStyle, configurable: true },
  requestAnimationFrame: {
    value: (callback: FrameRequestCallback) => {
      callback(0);
      return 1;
    },
    configurable: true,
  },
  cancelAnimationFrame: { value: () => undefined, configurable: true },
});

Object.defineProperties(dom.window.HTMLDialogElement.prototype, {
  showModal: {
    value(this: HTMLDialogElement) {
      this.setAttribute("open", "");
    },
    configurable: true,
  },
  close: {
    value(this: HTMLDialogElement) {
      this.removeAttribute("open");
      this.dispatchEvent(new dom.window.Event("close"));
    },
    configurable: true,
  },
});
