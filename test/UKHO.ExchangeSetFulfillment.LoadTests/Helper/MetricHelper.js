import { Trend, Counter, Rate } from 'k6/metrics';

// Job creation metrics
export const JobCreationTrend = {
  Small: new Trend('SmallJobCreationTime'),
  Medium: new Trend('MediumJobCreationTime')
};

// Job build metrics
export const JobBuildTrend = {
  Small: new Trend('SmallJobBuildTime'),
  Medium: new Trend('MediumJobBuildTime')
};

// End-to-end job metrics
export const JobTotalTrend = {
  Small: new Trend('SmallJobTotalTime'),
  Medium: new Trend('MediumJobTotalTime')
};

// API response time metrics
export const APIResponseTrend = {
  JobCreate: new Trend('JobCreateResponseTime'),
  JobStatus: new Trend('JobStatusResponseTime'),
  JobBuild: new Trend('JobBuildResponseTime')
};

// Builder step metrics for detailed analysis
export const BuilderStepTrend = {};

// Initialize builder step metrics for each known step
const knownBuilderSteps = [
  'ReadConfigurationNode',
  'GetBuildNode',
  'StartTomcatNode',
  'CheckEndpointsNode',
  'ProductSearchNode',
  'DownloadFilesNode',
  'AddExchangeSetNode',
  'AddContentExchangeSetNode',
  'SignExchangeSetNode',
  'ExtractExchangeSetNode',
  'UploadFilesNode'
];

knownBuilderSteps.forEach(step => {
  BuilderStepTrend[step] = new Trend(`BuilderStep_${step}`);
});

// Success/failure counters
export const JobStatusCounter = {
  Success: new Counter('SuccessfulJobs'),
  Failed: new Counter('FailedJobs'),
  Timeout: new Counter('TimedOutJobs')
};

// Job creation rate
export const JobCreationRate = new Rate('JobCreationRate');

/**
* Records metrics for job creation
* @param {string} jobSize - The size category of the job (Small, Medium, Large)
* @param {number} duration - Duration in milliseconds
*/
export function recordJobCreationTime(jobSize, duration) {
  if (JobCreationTrend[jobSize]) {
    JobCreationTrend[jobSize].add(duration);
    JobCreationRate.add(1);
  }
}

/**
* Records metrics for job build time
* @param {string} jobSize - The size category of the job (Small, Medium, Large)
* @param {number} duration - Duration in milliseconds
*/
export function recordJobBuildTime(jobSize, duration) {
  if (JobBuildTrend[jobSize]) {
    JobBuildTrend[jobSize].add(duration);
  }
}

/**
* Records metrics for total job time (end-to-end)
* @param {string} jobSize - The size category of the job (Small, Medium, Large)
* @param {number} duration - Duration in seconds
*/
export function recordJobTotalTime(jobSize, duration) {
  if (JobTotalTrend[jobSize]) {
    JobTotalTrend[jobSize].add(duration);
  }
}

/**
* Records metrics for API response times
* @param {string} apiType - The type of API call (JobCreate, JobStatus, JobBuild)
* @param {number} duration - Duration in milliseconds
*/
export function recordAPIResponseTime(apiType, duration) {
  if (APIResponseTrend[apiType]) {
    APIResponseTrend[apiType].add(duration);
  }
}

/**
* Records metrics for builder steps
* @param {Array} builderSteps - Array of builder steps from API response
*/
export function recordBuilderStepMetrics(builderSteps) {
  if (!builderSteps || !Array.isArray(builderSteps)) {
    return;
  }

  builderSteps.forEach(step => {
    if (step.nodeId && BuilderStepTrend[step.nodeId]) {
      BuilderStepTrend[step.nodeId].add(step.elapsedMilliseconds);
    }
  });
}

/**
* Records job status outcome
* @param {string} status - Job status (success, error, timeout)
*/
export function recordJobStatus(status) {
  switch(status.toLowerCase()) {
    case 'success':
      JobStatusCounter.Success.add(1);
      break;
    case 'error':
      JobStatusCounter.Failed.add(1);
      break;
    case 'timeout':
      JobStatusCounter.Timeout.add(1);
      break;
  }
}

/**
* Calculate total build time from builder steps
* @param {Array} builderSteps - Array of builder steps
* @returns {number} - Total build time in milliseconds
*/
export function calculateBuildTime(builderSteps) {
  if (!builderSteps || !Array.isArray(builderSteps)) {
    return 0;
  }

  return builderSteps.reduce((total, step) => total + step.elapsedMilliseconds, 0);
}

/**
* Analyzes builder steps to identify bottlenecks
* @param {Array} builderSteps - Array of builder steps
* @returns {Object} - Analysis results with slowest steps
*/
export function analyzeBuilderSteps(builderSteps) {
  if (!builderSteps || !Array.isArray(builderSteps) || builderSteps.length === 0) {
    return { bottlenecks: [] };
  }

  // Sort steps by elapsed time (descending)
  const sortedSteps = [...builderSteps].sort((a, b) => 
    b.elapsedMilliseconds - a.elapsedMilliseconds
  );

  const totalTime = calculateBuildTime(builderSteps);

  // Identify steps that take more than 10% of total time
  const bottlenecks = sortedSteps
    .filter(step => (step.elapsedMilliseconds / totalTime) > 0.1)
    .map(step => ({
      nodeId: step.nodeId,
      elapsedMilliseconds: step.elapsedMilliseconds,
      percentageOfTotal: (step.elapsedMilliseconds / totalTime * 100).toFixed(2) + '%'
    }));

  return {
    totalBuildTime: totalTime,
    bottlenecks: bottlenecks,
    slowestStep: sortedSteps[0]
  };
}