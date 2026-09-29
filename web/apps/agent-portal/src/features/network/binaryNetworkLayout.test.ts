import { strict as assert } from "node:assert";
import test from "node:test";
import type { BinaryTreeNodeDto } from "@modular-mlm/contracts";
import { buildBinaryNetworkGraph } from "./binaryNetworkLayout";

const root: BinaryTreeNodeDto = {
  agentId: "root",
  agentCode: "AG-ROOT",
  referralCode: "REF-ROOT",
  status: 2,
  parentAgentId: null,
  side: null,
  depth: 0,
};

test("binary layout keeps left and right children on their assigned sides", () => {
  const graph = buildBinaryNetworkGraph([
    root,
    child("left", 0),
    child("right", 1),
  ]);
  const rootX = positionX(graph.nodes, "root");

  assert.ok(positionX(graph.nodes, "left") < rootX);
  assert.ok(positionX(graph.nodes, "right") > rootX);
  assert.equal(graph.edges.length, 2);
});

test("binary layout inserts an opposite placeholder for a lone right child", () => {
  const graph = buildBinaryNetworkGraph([root, child("right", 1)]);
  const rootX = positionX(graph.nodes, "root");
  const placeholder = graph.nodes.find(
    (node) => node.data.kind === "open-position",
  );

  assert.ok(placeholder);
  assert.equal(placeholder.data.kind, "open-position");
  assert.equal(placeholder.data.side, 0);
  assert.ok(placeholder.position.x < rootX);
  assert.ok(positionX(graph.nodes, "right") > rootX);
});

function child(agentId: string, side: 0 | 1): BinaryTreeNodeDto {
  return {
    agentId,
    agentCode: `AG-${agentId.toUpperCase()}`,
    referralCode: `REF-${agentId.toUpperCase()}`,
    status: 2,
    parentAgentId: "root",
    side,
    depth: 1,
  };
}

function positionX(
  nodes: ReturnType<typeof buildBinaryNetworkGraph>["nodes"],
  id: string,
) {
  const node = nodes.find((candidate) => candidate.id === id);
  assert.ok(node);
  return node.position.x;
}
