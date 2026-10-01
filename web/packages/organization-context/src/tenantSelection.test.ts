import assert from "node:assert/strict";
import test from "node:test";
import {
  isLocalReturnPath,
  normalizeOrganizationSlug,
  organizationSelectionUrl,
} from "./tenantSelection";

test("organization selection normalizes safe tenant slugs", () => {
  assert.equal(normalizeOrganizationSlug(" Aurevia-PH "), "aurevia-ph");
  assert.equal(normalizeOrganizationSlug("aurevia/ph"), undefined);
  assert.equal(normalizeOrganizationSlug("-aurevia"), undefined);
});

test("organization selection URLs retain only local return paths", () => {
  assert.equal(
    organizationSelectionUrl("aurevia", "/catalog"),
    "/select-organization?slug=aurevia&returnTo=%2Fcatalog",
  );
  assert.equal(
    organizationSelectionUrl("aurevia", "//malicious.example"),
    "/select-organization?slug=aurevia&returnTo=%2F",
  );
  assert.equal(isLocalReturnPath("/orders"), true);
  assert.equal(isLocalReturnPath("https://malicious.example"), false);
});
