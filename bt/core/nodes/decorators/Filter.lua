
-----------------------------------------------------------------------------------------
-- Decorators
-- Filter

-- Filters the access of it's child node either a specific number of times, or every 
-- specific amount of time. By default the node is 'Treated as Inactive' to it's parent 
-- when child is Filtered. Unchecking this option will instead return Failure when Filtered.
-----------------------------------------------------------------------------------------

local Filter = bt.Class("Filter",bt.BTDecorator)
bt.Filter = Filter

FilterMode =
{
    LimitNumberOfTimes  = 0,
    CoolDown            = 1,
}

Policy =
{
    SuccessOrFailure    = 0,
    SuccessOnly         = 1,
    FailureOnly         = 2,
}
        
function Filter:ctor()
    bt.BTDecorator.ctor(self)
    self.name = "Filter"
    self.filterMode = FilterMode.CoolDown
    self.maxCount = 1
    self.coolDownTime = 5
    self.inactiveWhenLimited = true
    self.policy = Policy.SuccessOrFailure
    self.executedCount = 0
    self.currentTime = 0
    self.cooldownFuncId = 0
end

function Filter:init(jsonData)
    if jsonData.filterMode ~= nil then
        self.filterMode = FilterMode[jsonData.filterMode]
    end
    if jsonData.maxCount ~= nil then
        self.maxCount = jsonData.maxCount._value
    end
    if jsonData.coolDownTime ~= nil then
        self.coolDownTime = jsonData.coolDownTime._value
    end
    if jsonData.inactiveWhenLimited ~= nil then
        self.inactiveWhenLimited = jsonData.inactiveWhenLimited
    end
    if jsonData.policy ~= nil then
        self.policy = Policy[jsonData.policy]
    end
end

function Filter:onExecute(agent,blackboard)
    local decoratedConnection = self:getDecoratedConnection()
    if decoratedConnection == nil then
        return bt.Status.Resting
    end
    if self.filterMode == FilterMode.CoolDown then
        if self.currentTime > 0 then
            if self.inactiveWhenLimited then
                return bt.Status.Optional
            else
                return bt.Status.Failure
            end
        end
        self.status = decoratedConnection:execute(agent,blackboard)
        if self.status == bt.Status.Success or self.status == bt.Status.Failure then
            self.currentTime = self.coolDownTime
            self.cooldownFuncId = bt.addLoopFunc(self.coolDown,self)
        end
    elseif self.filterMode == FilterMode.LimitNumberOfTimes then
        if self.executedCount > self.maxCount then
            if self.inactiveWhenLimited then
                return bt.Status.Optional
            else
                return bt.Status.Failure
            end
        end
        self.status = decoratedConnection:execute(agent,blackboard)
        if (self.status == bt.Status.Success and self.policy == Policy.SuccessOnly) or
           (self.status == bt.Status.Failure and self.policy == Policy.FailureOnly) or 
           ((self.status == bt.Status.Success or self.status == bt.Status.Failure) 
           and self.policy == Policy.SuccessOrFailure) then
            self.executedCount = self.executedCount + 1
            self:debug(self.executedCount)
        end
    end
    return self.status
end

function Filter:onGraphStarted()
    self.executedCount = 0
    self.currentTime = 0
end

function Filter:coolDown()
    if self.cooldownFuncId then
        self.currentTime = self.currentTime - bt.deltaTime
        self:debug(string.format( "cooldown:%.2f/%.2f",self.currentTime,self.coolDownTime))
        if self.currentTime <= 0 then
            self.currentTime = 0
            bt.delLoopFunc(self.cooldownFuncId)
            self.cooldownFuncId = 0
        end
    end
end

function Filter:destroy()
    if self.cooldownFuncId > 0 then
        bt.delLoopFunc(self.cooldownFuncId)
    end
end
