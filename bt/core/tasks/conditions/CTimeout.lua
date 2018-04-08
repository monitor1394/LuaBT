local CTimeout = bt.Class("CTimeout",bt.ConditionTask)
bt.CTimeout = CTimeout

function CTimeout:ctor()
    bt.ConditionTask.ctor(self)
    self.name = "CTimeout"
    self.timeout = 1
    self.currentTime = 0

    self.timeoutFuncId = 0
end

function CTimeout:init(jsonData)
    bt.ConditionTask:init(jsonData)
    if jsonData.timeout then
        self.timeout = jsonData.timeout._value
    end
end

function CTimeout:onCheck()
    if self.timeoutFuncId <= 0 then
        self.currentTime = 0
        self.timeoutFuncId = bt.addLoopFunc(self.checkTimeout,self)
    end
    if self.currentTime >= self.timeout then
        bt.delLoopFunc(self.timeoutFuncId)
        self.timeoutFuncId = 0
        return true
    else 
        return false
    end
end

function CTimeout:checkTimeout()
    if self.timeoutFuncId > 0 then
        self.currentTime = self.currentTime + bt.deltaTime
        self:debug()
    end
end

function CTimeout:info()
    return string.format("If CTimeout %.2f/%.2f sec.",self.currentTime,self.timeout)
end

function CTimeout:destroy()
    if self.timeoutFuncId > 0 then
        bt.delLoopFunc(self.timeoutFuncId)
    end
end