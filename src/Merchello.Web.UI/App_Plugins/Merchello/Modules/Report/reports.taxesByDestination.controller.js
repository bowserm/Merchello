﻿(function (controllers, undefined) {
   
    /**
     * @ngdoc controller
     * @name Merchello.Dashboards.Report.TaxesByDestinationController
     * @function
     * 
     * @description
     * The controller for the reports TaxesByDestination page
     */
    controllers.TaxesByDestinationController = function ($scope, $routeParams, $location, notificationsService, angularHelper, serverValidationManager, merchelloProductService) {

        $scope.loaded = true;
        $scope.preValuesLoaded = true;

    }


    angular.module("umbraco").controller("Merchello.Dashboards.Report.TaxesByDestinationController", merchello.Controllers.TaxesByDestinationController);


}(window.merchello.Controllers = window.merchello.Controllers || {}));
