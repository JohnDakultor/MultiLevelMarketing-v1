import assert from "node:assert/strict";
import test from "node:test";
import {
  capRulesJson,
  qualificationRulesJson,
  readCapRules,
  readQualificationRules,
} from "./CompensationPage";

test("administrator qualification controls serialize to the backend rule shape", () => {
  const form = new FormData();
  form.set("requireActiveAgent", "on");
  form.set("requiredQualificationState", "Qualified");
  form.set("minimumPersonalSales", "1000");
  form.set("minimumPersonalBusinessVolume", "500");
  form.set("minimumActiveDirectRecruits", "2");
  form.set("requireActiveLeftLeg", "on");

  assert.deepEqual(JSON.parse(qualificationRulesJson(form)), {
    requireActiveAgent: true,
    requiredQualificationState: "Qualified",
    minimumPersonalSales: 1000,
    minimumPersonalBusinessVolume: 500,
    minimumActiveDirectRecruits: 2,
    requireActiveLeftLeg: true,
    requireActiveRightLeg: false,
  });
});

test("administrator cap controls serialize enabled and disabled limits safely", () => {
  const enabled = new FormData();
  enabled.set("capEnabled", "on");
  enabled.set("maximumCapAmount", "50000");
  assert.deepEqual(JSON.parse(capRulesJson(enabled)), {
    enabled: true,
    maximumAmount: 50000,
  });

  assert.deepEqual(JSON.parse(capRulesJson(new FormData())), {
    enabled: false,
    maximumAmount: 0,
  });
});

test("legacy empty rule sentinels populate safe form defaults", () => {
  assert.equal(readQualificationRules("[]").minimumPersonalSales, 0);
  assert.equal(readQualificationRules("{}").requireActiveAgent, false);
  assert.deepEqual(readCapRules("[]"), { enabled: false, maximumAmount: 0 });
});

test("existing PascalCase backend JSON remains editable", () => {
  assert.equal(
    readQualificationRules('{"RequireActiveAgent":true}').requireActiveAgent,
    true,
  );
  assert.deepEqual(readCapRules('{"Enabled":true,"MaximumAmount":2500}'), {
    enabled: true,
    maximumAmount: 2500,
  });
});
