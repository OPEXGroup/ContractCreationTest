pragma solidity ^0.4.18;

contract Test {
    address public owner;

    constructor() public {
        owner = msg.sender;
    }
}