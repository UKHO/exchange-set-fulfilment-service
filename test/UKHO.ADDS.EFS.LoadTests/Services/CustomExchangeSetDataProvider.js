import { randomIntBetween } from 'https://jslib.k6.io/k6-utils/1.4.0/index.js';
// Load config and products
const config = JSON.parse(open('../Config.json'));

export function getSmallProductNamesData() {
    const filters = config.CustomExchangeSetData.smallProductNames;
    return filters[Math.floor(randomIntBetween(0, filters.length - 1))];
}

export function getMediumProductNamesData() {
    const filters = config.CustomExchangeSetData.mediumProductNames;
    return filters[Math.floor(randomIntBetween(0, filters.length - 1))];
}

export function getSmallProductVersionsData() {
    const filters = config.CustomExchangeSetData.smallProductVersions;
    return filters[Math.floor(randomIntBetween(0, filters.length - 1))];
}

export function getMediumProductVersionsData() {
    const filters = config.CustomExchangeSetData.mediumProductVersions;
    return filters[Math.floor(randomIntBetween(0, filters.length - 1))];
}

export function getSmallUpdateSinceData() {
    const filters = config.CustomExchangeSetData.smallUpdateSince;
    return filters[Math.floor(randomIntBetween(0, filters.length - 1))];
}

export function getMediumUpdateSinceData() {
    const filters = config.CustomExchangeSetData.mediumUpdateSince;
    return filters[Math.floor(randomIntBetween(0, filters.length - 1))];
}
