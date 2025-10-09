import { randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';
// Load config and products
const config = JSON.parse(open('../Config.json'));

/**
* Gets a random filter for small jobs
* @returns {string} - A filter for small jobs
*/
export function getSmallJobFilter() {
  const filters = config.FilterData.Small;
  return filters[Math.floor(randomIntBetween(0, filters.length - 1))];
}

/**
* Gets a random filter for medium jobs
* @returns {string} - A filter for medium jobs
*/
export function getMediumJobFilter() {
  const filters = config.FilterData.Medium;
  return filters[Math.floor(randomIntBetween(0, filters.length - 1))];
}