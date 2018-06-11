
-----------------------------------------------------------------------------------------
-- Decorators
-- Repeater

-- Repeat the child either x times or until it returns the specified status, or forever
-----------------------------------------------------------------------------------------

local Repeater = bt.Class("Repeater",bt.BTDecorator)
bt.Repeater = Repeater
    
RepeaterMode =
{
    RepeatTimes     = 0,
    RepeatUntil     = 1,
    RepeatForever   = 2,
}

RepeatUntilStatus =
{
    Failure         = 0,
    Success         = 1,
}


function Repeater:ctor()
    bt.BTDecorator.ctor(self)
    self.name = "Repeater"
    self.repeaterMode = RepeaterMode.RepeatTimes
    self.repeatUntilStatus = RepeatUntilStatus.Success
    self.repeatTimes = 1
    self.currentInteration = 1
end

function Repeater:init(jsonData)
    if jsonData.repeaterMode ~= nil then
        self.repeaterMode = RepeaterMode[jsonData.repeaterMode]
    end
    if jsonData.repeatUntilStatus ~= nil then
        self.repeatUntilStatus = RepeatUntilStatus[jsonData.repeatUntilStatus]
    end
    if jsonData.repeatTimes ~= nil then
        self.repeatTimes = jsonData.repeatTimes._value
    end
end

function Repeater:onExecute(agent,blackboard)
    local decoratedConnection = self:getDecoratedConnection()
    if decoratedConnection == nil then
        return bt.Status.Resting
    end
    if decoratedConnection.status == bt.Status.Success or decoratedConnection.status == bt.Status.Failure then
        decoratedConnection:reset(true)
    end
    self.status = decoratedConnection:execute(agent,blackboard)

    if self.status == bt.Status.Resting then
        return bt.Status.Running
    elseif self.status == bt.Status.Running then
        return bt.Status.Running
    end

    if self.repeaterMode == RepeaterMode.RepeatTimes then
        self:debug(string.format("times:%d/%d",self.currentInteration,self.repeatTimes))
        if self.currentInteration >= self.repeatTimes then
            return self.status
        end
        self.currentInteration = self.currentInteration + 1

    elseif self.repeaterMode == RepeaterMode.RepeatUntil then
        self:debug(string.format("status:%s/%s",bt.getStatusInfo(self.status),bt.getStatusInfo(self.repeatUntilStatus)))
        if self.status == self.repeatUntilStatus then
            return status
        end
    end

    return bt.Status.Running
end

function Repeater:onReset()
    self.currentInteration = 1
end

function Repeater:destroy()
end
