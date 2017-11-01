window.__xcb_Bridge = {
    createObject: function () {
        return {};
    },

    createArray: function () {
        return [];
    },

    createFunction: function (functionId) {
        return function () {
            return window.external.__xcb_InvokeProxy (functionId, this, arguments);
        }
    },

    getProperty: function (target, propertyName) {
        return target[propertyName];
    },

    getPropertyNames: function (target) {
        var prototypePropertyNames = Object.getOwnPropertyNames (Object.getPrototypeOf (target))
            .filter (function (p) { return p !== "constructor"; }); // To match behavior of JSC GetPropertyNames
        return Object.getOwnPropertyNames (target).concat (prototypePropertyNames);
    },

    hasProperty: function (target, propertyName) {
        return target.hasOwnProperty (propertyName) || Object.getPrototypeOf (target).hasOwnProperty (propertyName);
    },

    setProperty: function (target, propertyName, value) {
        target[propertyName] = value;
    },

    applyFunction: function () {
        return arguments.length == 1
          ? arguments[0].apply (null)
          : arguments[0].apply (null, Array.prototype.slice.call (arguments, 1));
    }
};

window.__xcb_getBridge = function () {
    return window.__xcb_Bridge;
};