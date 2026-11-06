export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresInSeconds: number;
}

export interface Attributes {
  for: number;
  int: number;
  sab: number;
  vit: number;
}

export interface PlayerStatus {
  id: number;
  username: string;
  level: number;
  currentXp: number;
  xpToNextLevel: number;
  attributes: Attributes;
  dailyDeadline: string;
}

export type QuestStatus = "PENDING" | "COMPLETED" | "FAILED";
export type AttributeType = "STR" | "INT" | "WIS" | "VIT";
export type QuestKind = "NORMAL" | "PENALTY" | "BONUS";
export type ProofType =
  | "STRAVA"
  | "SUMMARY"
  | "MINDMAP"
  | "PHOTO"
  | "PAYMENT_RECEIPT"
  | "GENERIC";

export interface Quest {
  id: number;
  goalId: number | null;
  title: string;
  description: string | null;
  metricTarget: string | null;
  proofType: ProofType;
  estimatedMinutes: number;
  attributeType: AttributeType;
  xpReward: number;
  status: QuestStatus;
  kind: QuestKind;
  isPenalty: boolean;
  targetDate: string;
  hasProof: boolean;
}

export type GoalCategory = "RUNNING" | "STUDY" | "READING" | "GENERIC";
export type GoalStatus = "ACTIVE" | "COMPLETED" | "PAUSED";

export interface Goal {
  id: number;
  title: string;
  description: string | null;
  attributeType: AttributeType;
  category: GoalCategory;
  targetMetric: string | null;
  contentBacklog: string | null;
  status: GoalStatus;
  progressNotes: string | null;
  startDate: string;
  targetDate: string | null;
}

/** Payload de criação/edição (sem id). */
export type GoalInput = Omit<Goal, "id">;

export type PunishmentCategory = "FISICO" | "ROTINA" | "FINANCEIRO";

export interface Punishment {
  id: number;
  code: string;
  category: PunishmentCategory;
  title: string;
  description: string;
  proofType: ProofType;
  estimatedMinutes: number;
  xpReward: number;
  attributeType: AttributeType;
  weight: number;
  isActive: boolean;
}

/** Payload de criação/edição (sem id). */
export type PunishmentInput = Omit<Punishment, "id">;

// --- Evolução: traços Big Five e métricas (Fase 1) ---

export type BigFiveTrait =
  | "OPENNESS"
  | "CONSCIENTIOUSNESS"
  | "EXTRAVERSION"
  | "AGREEABLENESS"
  | "NEUROTICISM";

export interface Trait {
  trait: BigFiveTrait;
  /** 0-100, 50 = neutro. Em NEUROTICISM, menor é melhor. */
  value: number;
  updatedAt: string;
}

export interface TraitHistoryPoint {
  trait: BigFiveTrait;
  value: number;
  delta: number;
  recordedAt: string;
}

export interface PersonalityResponse {
  traits: Trait[];
  history: TraitHistoryPoint[];
}

export interface MetricPoint {
  recordedAt: string;
  value: number;
}

export interface MetricSeries {
  metricName: string;
  unit: string | null;
  points: MetricPoint[];
}

export interface MetricsResponse {
  series: MetricSeries[];
}

// --- Reflexão noturna (Fase 2) ---

export interface Reflection {
  id: number;
  reflectionDate: string;
  autopilotText: string;
  idealText: string | null;
  mentorFeedback: string | null;
}

export interface ReflectionInput {
  autopilotText: string;
  idealText?: string;
}

// --- Briefing de notícias (Fase 3) ---

export interface BriefingItem {
  source: string;
  title: string;
  url: string | null;
  comment: string;
}

export interface Briefing {
  briefingDate: string;
  summaryMd: string;
  sourceCount: number;
  items: BriefingItem[];
}
