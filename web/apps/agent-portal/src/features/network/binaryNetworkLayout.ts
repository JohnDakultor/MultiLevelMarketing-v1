import type { BinaryTreeNodeDto } from "@modular-mlm/contracts";
import type { Edge, Node } from "@xyflow/react";
import { hierarchy, tree } from "d3-hierarchy";

export interface AgentNetworkNodeData extends Record<string, unknown> {
  kind: "agent";
  agent: BinaryTreeNodeDto;
}

export interface OpenPositionNodeData extends Record<string, unknown> {
  kind: "open-position";
  side: 0 | 1;
}

export type BinaryNetworkFlowNode =
  | Node<AgentNetworkNodeData, "agent">
  | Node<OpenPositionNodeData, "open-position">;

interface LayoutDatum {
  id: string;
  side: 0 | 1 | null;
  data: AgentNetworkNodeData | OpenPositionNodeData;
  children: LayoutDatum[];
}

const nodeWidth = 220;
const nodeHeight = 112;

export function buildBinaryNetworkGraph(agents: readonly BinaryTreeNodeDto[]): {
  nodes: BinaryNetworkFlowNode[];
  edges: Edge[];
} {
  if (agents.length === 0) return { nodes: [], edges: [] };

  const rootAgent =
    agents.find((agent) => agent.depth === 0) ??
    agents.reduce((current, agent) =>
      agent.depth < current.depth ? agent : current,
    );
  const records = new Map<string, LayoutDatum>();

  for (const agent of agents) {
    records.set(agent.agentId, {
      id: agent.agentId,
      side: normalizeSide(agent.side),
      data: { kind: "agent", agent },
      children: [],
    });
  }

  for (const agent of agents) {
    if (agent.agentId === rootAgent.agentId || !agent.parentAgentId) continue;
    const parent = records.get(agent.parentAgentId);
    const child = records.get(agent.agentId);
    if (parent && child) parent.children.push(child);
  }

  for (const parent of records.values()) {
    if (parent.children.length !== 1) continue;
    const occupiedSide = parent.children[0]?.side;
    if (occupiedSide === null || occupiedSide === undefined) continue;
    const openSide = occupiedSide === 0 ? 1 : 0;
    parent.children.push({
      id: `open-${parent.id}-${openSide}`,
      side: openSide,
      data: { kind: "open-position", side: openSide },
      children: [],
    });
  }

  for (const parent of records.values()) {
    parent.children.sort(
      (left, right) => sideOrder(left.side) - sideOrder(right.side),
    );
  }

  const root = records.get(rootAgent.agentId);
  if (!root) return { nodes: [], edges: [] };

  const layout = tree<LayoutDatum>()
    .nodeSize([nodeWidth + 56, nodeHeight + 72])
    .separation((left, right) => (left.parent === right.parent ? 1 : 1.15));
  const positionedRoot = layout(hierarchy(root, (datum) => datum.children));
  const nodes: BinaryNetworkFlowNode[] = positionedRoot
    .descendants()
    .map((positioned) => {
      const common = {
        id: positioned.data.id,
        position: {
          x: positioned.x - nodeWidth / 2,
          y: positioned.y,
        },
        draggable: false,
        connectable: false,
        selectable: positioned.data.data.kind === "agent",
        focusable: true,
        width: nodeWidth,
      };

      return positioned.data.data.kind === "agent"
        ? {
            ...common,
            type: "agent" as const,
            data: positioned.data.data,
            ariaLabel: `${positioned.data.data.agent.agentCode}, ${sideLabel(positioned.data.side)} placement`,
          }
        : {
            ...common,
            type: "open-position" as const,
            data: positioned.data.data,
            ariaLabel: `Open ${sideLabel(positioned.data.side)} position`,
          };
    });
  const edges: Edge[] = positionedRoot
    .descendants()
    .filter((positioned) => positioned.parent !== null)
    .map((positioned) => ({
      id: `${positioned.parent!.data.id}-${positioned.data.id}`,
      source: positioned.parent!.data.id,
      target: positioned.data.id,
      type: "smoothstep",
      ariaLabel: `${sideLabel(positioned.data.side)} placement connection`,
      style: {
        stroke: positioned.data.side === 0 ? "#2563eb" : "#d97706",
        strokeWidth: 2,
      },
    }));

  return { nodes, edges };
}

function normalizeSide(side: number | null): 0 | 1 | null {
  if (side === 0 || side === 1) return side;
  return null;
}

function sideOrder(side: 0 | 1 | null) {
  if (side === 0) return 0;
  if (side === 1) return 1;
  return 2;
}

function sideLabel(side: 0 | 1 | null) {
  if (side === 0) return "left";
  if (side === 1) return "right";
  return "root";
}
