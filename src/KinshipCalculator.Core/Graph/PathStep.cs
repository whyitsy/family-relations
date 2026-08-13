using KinshipCalculator.Core.Models;

namespace KinshipCalculator.Core.Graph;

/// <summary>路径中的一步：到达的人及其关系种类（显式携带种类，以区分近亲导致的多种关系）。</summary>
public readonly record struct PathStep(StepKind Kind, Person Person);
