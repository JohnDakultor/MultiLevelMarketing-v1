"use client";

import type { BinaryTreeNodeDto } from "@modular-mlm/contracts";
import { Badge, Card } from "@modular-mlm/design-system";
import {
  Background,
  BackgroundVariant,
  Controls,
  Handle,
  MiniMap,
  Position,
  ReactFlow,
  type NodeProps,
} from "@xyflow/react";
import { useMemo, useState } from "react";
import { AgentStatusBadge } from "../shared/status";
import {
  buildBinaryNetworkGraph,
  type AgentNetworkNodeData,
  type BinaryNetworkFlowNode,
  type OpenPositionNodeData,
} from "./binaryNetworkLayout";

type AgentFlowNode = Extract<BinaryNetworkFlowNode, { type?: "agent" }>;
type OpenPositionFlowNode = Extract<
  BinaryNetworkFlowNode,
  { type?: "open-position" }
>;

const nodeTypes = {
  agent: AgentNetworkNode,
  "open-position": OpenPositionNode,
};

export function BinaryNetworkGraph({
  agents,
}: {
  agents: BinaryTreeNodeDto[];
}) {
  const [selectedAgentId, setSelectedAgentId] = useState<string | null>(null);
  const graph = useMemo(() => buildBinaryNetworkGraph(agents), [agents]);
  const selectedAgent = agents.find(
    (agent) => agent.agentId === selectedAgentId,
  );
  const selectedNodes = useMemo(
    () =>
      graph.nodes.map((node) => ({
        ...node,
        selected: node.id === selectedAgentId,
      })),
    [graph.nodes, selectedAgentId],
  );

  return (
    <div className="binary-network">
      <div className="binary-network__legend" aria-label="Network legend">
        <span>
          <i className="binary-network__legend-line binary-network__legend-line--left" />
          Left leg
        </span>
        <span>
          <i className="binary-network__legend-line binary-network__legend-line--right" />
          Right leg
        </span>
        <span>
          <i className="binary-network__legend-open" />
          Open position
        </span>
      </div>
      <div
        className="binary-network__canvas"
        aria-label="Interactive binary placement network"
      >
        <ReactFlow
          nodes={selectedNodes}
          edges={graph.edges}
          nodeTypes={nodeTypes}
          nodesDraggable={false}
          nodesConnectable={false}
          elementsSelectable
          edgesFocusable={false}
          fitView
          fitViewOptions={{ padding: 0.2, maxZoom: 1 }}
          minZoom={0.2}
          maxZoom={1.5}
          onlyRenderVisibleElements
          onNodeClick={(_, node) => {
            if (node.data.kind === "agent") setSelectedAgentId(node.id);
          }}
          onPaneClick={() => setSelectedAgentId(null)}
          aria-label="Binary placement tree"
        >
          <Background variant={BackgroundVariant.Dots} gap={20} size={1} />
          <MiniMap
            pannable
            zoomable
            ariaLabel="Binary network minimap"
            nodeColor={(node) => {
              const data = node.data as
                AgentNetworkNodeData | OpenPositionNodeData;

              return data.kind === "open-position"
                ? "#d1d5db"
                : data.agent.side === 0
                  ? "#2563eb"
                  : data.agent.side === 1
                    ? "#d97706"
                    : "#0f766e";
            }}
          />
          <Controls showInteractive={false} />
        </ReactFlow>
      </div>
      {selectedAgent && <SelectedAgent agent={selectedAgent} />}
      <p className="binary-network__help">
        Drag the canvas to navigate, use the controls to zoom, and select an
        Agent for placement details. Network placement is read-only.
      </p>
    </div>
  );
}

function AgentNetworkNode({ data, selected }: NodeProps<AgentFlowNode>) {
  const { agent } = data as AgentNetworkNodeData;
  const side = agent.side === 0 ? "Left" : agent.side === 1 ? "Right" : "Root";
  return (
    <div
      className={`binary-network-node${selected ? " binary-network-node--selected" : ""}`}
    >
      <Handle type="target" position={Position.Top} isConnectable={false} />
      <div className="binary-network-node__header">
        <strong>{agent.agentCode}</strong>
        <Badge>{side}</Badge>
      </div>
      <span className="binary-network-node__referral">
        Referral {agent.referralCode}
      </span>
      <AgentStatusBadge value={agent.status} />
      <Handle type="source" position={Position.Bottom} isConnectable={false} />
    </div>
  );
}

function OpenPositionNode({ data }: NodeProps<OpenPositionFlowNode>) {
  const open = data as OpenPositionNodeData;
  return (
    <div className="binary-network-open-position">
      <Handle type="target" position={Position.Top} isConnectable={false} />
      <span>Open {open.side === 0 ? "left" : "right"} position</span>
    </div>
  );
}

function SelectedAgent({ agent }: { agent: BinaryTreeNodeDto }) {
  return (
    <Card>
      <div className="binary-network__selection">
        <div>
          <span className="muted-label">Selected Agent</span>
          <h2>{agent.agentCode}</h2>
        </div>
        <AgentStatusBadge value={agent.status} />
        <dl>
          <div>
            <dt>Referral code</dt>
            <dd>{agent.referralCode}</dd>
          </div>
          <div>
            <dt>Level</dt>
            <dd>{agent.depth}</dd>
          </div>
          <div>
            <dt>Placement</dt>
            <dd>
              {agent.side === 0
                ? "Left leg"
                : agent.side === 1
                  ? "Right leg"
                  : "Network root"}
            </dd>
          </div>
        </dl>
      </div>
    </Card>
  );
}
