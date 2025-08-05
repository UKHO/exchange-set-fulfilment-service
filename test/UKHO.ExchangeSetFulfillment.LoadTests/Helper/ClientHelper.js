import http from 'k6/http';
import { check, group } from 'k6';
import { Trend } from 'k6/metrics';

const config = JSON.parse(open('../Config.json'));

// Trends for tracking job metrics
export const SmallJobCreationTime = new Trend('SmallJobCreationTime');
export const MediumJobCreationTime = new Trend('MediumJobCreationTime');

export const SmallJobBuildTime = new Trend('SmallJobBuildTime');
export const MediumJobBuildTime = new Trend('MediumJobBuildTime');

export const SmallJobTotalTime = new Trend('SmallJobTotalTime');
export const MediumJobTotalTime = new Trend('MediumJobTotalTime');

// Trends for tracking API response times
export const JobCreateResponseTime = new Trend('JobCreateResponseTime');
export const JobStatusResponseTime = new Trend('JobStatusResponseTime');
export const JobBuildResponseTime = new Trend('JobBuildResponseTime');

/**
* Creates a job with the given filter
* @param {string} filter - The filter to use for job creation
* @param {string} jobSize - The size category of the job (Small, Medium, Large)
* @returns {Object} - Response object with job details
*/
export function createJob(filter, jobSize) {
  const correlationId = generateCorrelationId(jobSize);
  const payload = JSON.stringify({
    version: 1,
    dataStandard: "s100",
    products: "",
    filter: filter
  });

  const url = `${config.Base_URL}${config.JobsEndpoint}`;

  const params = {
    headers: {
      'X-Correlation-Id': correlationId,
      'Content-Type': 'application/json'
    }
  };

  const response = http.post(url, payload, params);

  check(response, {
    'Job created successfully': (r) => r.status === 200,
    'Job ID is present': (r) => r.json().jobId !== undefined
  });

  JobCreateResponseTime.add(response.timings.duration);

  return {
    jobId: response.json().jobId,
    correlationId: correlationId,
    response: response
  };
}

/**
* Gets the status of a job
* @param {string} jobId - The ID of the job
* @returns {Object} - Response object with job status
*/
export function getJobStatus(jobId) {
  const url = `${config.Base_URL}${config.JobStatusEndpoint.replace('{jobId}', jobId)}`;

  const params = {
    headers: {
      'Content-Type': 'application/json'
    }
  };

  const response = http.get(url, params);

  check(response, {
    'Job status retrieved successfully': (r) => r.status === 200
  });

  JobStatusResponseTime.add(response.timings.duration);

  return {
    status: response.json().jobState,
    buildStatus: response.json().buildState,
    response: response
  };
}

/**
* Gets the build details of a job
* @param {string} jobId - The ID of the job
* @returns {Object} - Response object with build details
*/
export function getJobBuild(jobId) {
  const url = `${config.Base_URL}${config.JobBuildEndpoint.replace('{jobId}', jobId)}`;

  const params = {
    headers: {
      'Content-Type': 'application/json'
    }
  };

  const response = http.get(url, params);

  check(response, {
    'Build details retrieved successfully': (r) => r.status === 200
  });

  JobBuildResponseTime.add(response.timings.duration);

  return {
    builderExitCode: response.json().builderExitCode,
    builderSteps: response.json().builderSteps,
    response: response
  };
}

/**
* Generates a unique correlation ID
* @param {string} prefix - Prefix for the correlation ID
* @returns {string} - Generated correlation ID
*/
export function generateCorrelationId(prefix) {
  // Format: job-[size]-[timestamp with ms]-[random hex]
  const timestamp = new Date().getTime();

  // Generate 8 bytes (16 hex chars) of randomness
  const random = Array.from(
    { length: 16 }, 
    () => Math.floor(Math.random() * 16).toString(16)
  ).join('');

  return `job-${prefix.toLowerCase()}-${timestamp}-${random}`;
}

/**
* Get the duration of a function execution within a group
* @param {string} groupName - Name of the group
* @param {Function} fn - Function to execute
* @returns {number} - Duration in milliseconds
*/
export function getGroupDuration(groupName, fn) {
  const start = new Date();
  group(groupName, fn);
  return new Date() - start;
}

/**
* Calculate total build time from builder steps
* @param {Array} builderSteps - Array of builder steps
* @returns {number} - Total build time in milliseconds
*/
export function calculateBuildTime(builderSteps) {
  return builderSteps.reduce((total, step) => total + step.elapsedMilliseconds, 0);
}